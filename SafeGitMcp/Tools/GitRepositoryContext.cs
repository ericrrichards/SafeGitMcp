using System.Text;
using GitReader;
using GitReader.Structures;

namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryContext : IDisposable {
    private const int MaximumBlobBytes = 1_000_000;
    private readonly StructuredRepository _repository;
    private readonly string _workingDirectoryPath;

    private GitRepositoryContext(StructuredRepository repository, string workingDirectoryPath) {
        _repository = repository;
        _workingDirectoryPath = workingDirectoryPath;
    }

    public static async Task<GitRepositoryContext> CreateAsync(string[] args, CancellationToken cancellationToken = default) {
        var repositoryPath = GetRepositoryPathArgument(args);

        if (!Path.IsPathFullyQualified(repositoryPath)) {
            throw new ArgumentException("The repository path must be absolute.");
        }

        var fullPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(fullPath)) {
            throw new DirectoryNotFoundException($"The repository path does not exist: {fullPath}");
        }

        var repository = await Repository.Factory.OpenStructureAsync(fullPath, cancellationToken);
        return new GitRepositoryContext(repository, fullPath);
    }

    public async Task<CurrentChangesetFile[]> GetCurrentChangesetFilesAsync() {
        var status = await _repository.GetWorkingDirectoryStatusAsync(
            await GetChangesetIgnoreFilterAsync(),
            CancellationToken.None);
        var headBlobs = await GetHeadBlobHashesAsync();
        var filesByPath = new Dictionary<string, WorkingDirectoryFile>(StringComparer.Ordinal);
        AddFiles(filesByPath, status.StagedFiles);
        AddFiles(filesByPath, status.UnstagedFiles);
        AddFiles(filesByPath, status.UntrackedFiles);

        var files = new List<CurrentChangesetFile>(filesByPath.Count);
        foreach (var file in filesByPath.Values) {
            var hasBaseline = headBlobs.TryGetValue(file.Path, out var headHash);
            Hash? baselineHash = hasBaseline ? (Hash?)headHash : null;
            var baselineContent = await ReadBlobContentAsync(baselineHash);
            var currentContent = await ReadWorkingTreeContentAsync(file.Path);
            if (AreEquivalent(baselineContent, currentContent, baselineHash, file.WorkingTreeHash)) {
                continue;
            }

            files.Add(new CurrentChangesetFile(file, baselineHash, baselineContent, currentContent));
        }

        return [.. files];
    }

    public async Task<CommitReview?> GetCommitReviewAsync(Hash hash) {
        var commit = await _repository.GetCommitAsync(hash, CancellationToken.None);
        if (commit is null) {
            return null;
        }

        var parent = await commit.GetPrimaryParentCommitAsync(CancellationToken.None);
        var changes = await GetCommitChangesAsync(parent, commit);
        return new CommitReview(commit, parent, changes);
    }

    public async Task<CommitFileAtCommit?> GetCommitFileAsync(Hash commitHash, string path) {
        var commit = await _repository.GetCommitAsync(commitHash, CancellationToken.None);
        if (commit is null) {
            return null;
        }

        var root = await commit.GetTreeRootAsync(CancellationToken.None);
        var blobHash = FindBlobHash(root.Children, path.Split('/', StringSplitOptions.RemoveEmptyEntries), 0);
        if (blobHash is null || await ReadBlobContentAsync(blobHash) is not { } content) {
            return null;
        }

        return new CommitFileAtCommit(commit, path, blobHash.Value, content);
    }

    public Task<CommitHistorySearchResult> GetCommitsSinceShaAsync(Hash sha) {
        return GetCommitHistoryAsync(sha, null);
    }

    public async Task<CommitHistoryEntry[]> GetCommitsSinceTimestampAsync(DateTimeOffset timestamp) {
        return (await GetCommitHistoryAsync(null, timestamp)).Commits;
    }

    public void Dispose() {
        _repository.Dispose();
    }

    private async Task<GitBlobContent?> ReadBlobContentAsync(Hash? hash) {
        if (hash is null) {
            return null;
        }

        using var result = await _repository.OpenRawObjectStreamAsync(hash.Value, CancellationToken.None);
        return result.Type == ObjectTypes.Blob ? await ReadContentAsync(result.Stream) : null;
    }

    private static async Task<GitBlobContent> ReadContentAsync(Stream stream) {
        var buffer = new byte[MaximumBlobBytes + 1];
        var bytesRead = 0;
        while (bytesRead < buffer.Length) {
            var read = await stream.ReadAsync(buffer.AsMemory(bytesRead), CancellationToken.None);
            if (read == 0) {
                break;
            }

            bytesRead += read;
        }

        var isTruncated = bytesRead > MaximumBlobBytes;
        var content = buffer.AsSpan(0, Math.Min(bytesRead, MaximumBlobBytes));
        if (content.Contains((byte)0)) {
            return GitBlobContent.Binary(isTruncated, content.Length);
        }

        try {
            return GitBlobContent.TextContent(isTruncated, content.Length, new UTF8Encoding(false, true).GetString(content));
        } catch (DecoderFallbackException) {
            return GitBlobContent.Binary(isTruncated, content.Length);
        }
    }

    private async Task<GitBlobContent?> ReadWorkingTreeContentAsync(string relativePath) {
        var filePath = Path.GetFullPath(Path.Combine(_workingDirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var pathFromRepository = Path.GetRelativePath(_workingDirectoryPath, filePath);
        if (Path.IsPathFullyQualified(pathFromRepository) || pathFromRepository == ".." || pathFromRepository.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || !File.Exists(filePath)) {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await ReadContentAsync(stream);
    }

    private async Task<GlobFilter> GetChangesetIgnoreFilterAsync() {
        var commonFilter = Glob.GetCommonIgnoreFilter();
        var gitIgnorePath = Path.Combine(_workingDirectoryPath, ".gitignore");
        if (!File.Exists(gitIgnorePath)) {
            return commonFilter;
        }

        await using var stream = File.OpenRead(gitIgnorePath);
        var repositoryFilter = await Glob.CreateExcludeFilterFromGitignoreAsync(stream, CancellationToken.None);
        return Glob.Combine(commonFilter, repositoryFilter);
    }

    private async Task<Dictionary<string, Hash>> GetHeadBlobHashesAsync() {
        var head = _repository.Head;
        if (head is null) {
            return [];
        }

        return await GetCommitBlobHashesAsync(await head.GetHeadCommitAsync(CancellationToken.None));
    }

    private async Task<Dictionary<string, Hash>> GetCommitBlobHashesAsync(Commit commit) {
        var root = await commit.GetTreeRootAsync(CancellationToken.None);
        var blobs = new Dictionary<string, Hash>(StringComparer.Ordinal);
        AddTreeBlobs(root.Children, string.Empty, blobs);
        return blobs;
    }

    private static Hash? FindBlobHash(IEnumerable<TreeEntry> entries, string[] pathSegments, int segmentIndex) {
        if (segmentIndex >= pathSegments.Length) {
            return null;
        }

        var entry = entries.FirstOrDefault(entry => string.Equals(entry.Name, pathSegments[segmentIndex], StringComparison.Ordinal));
        if (entry is null) {
            return null;
        }

        if (segmentIndex == pathSegments.Length - 1) {
            return entry is TreeBlobEntry blob ? (Hash?)blob.Hash : null;
        }

        return entry is IParentTreeEntry directory
            ? FindBlobHash(directory.Children, pathSegments, segmentIndex + 1)
            : null;
    }

    private async Task<CommitFileChange[]> GetCommitChangesAsync(Commit? parent, Commit commit) {
        var baselineBlobs = parent is null ? new Dictionary<string, Hash>(StringComparer.Ordinal) : await GetCommitBlobHashesAsync(parent);
        var currentBlobs = await GetCommitBlobHashesAsync(commit);
        var paths = new HashSet<string>(baselineBlobs.Keys, StringComparer.Ordinal);
        paths.UnionWith(currentBlobs.Keys);

        var changes = new List<CommitFileChange>();
        foreach (var path in paths.OrderBy(path => path, StringComparer.Ordinal)) {
            var hasBaseline = baselineBlobs.TryGetValue(path, out var baselineBlob);
            var hasCurrent = currentBlobs.TryGetValue(path, out var currentBlob);
            if (hasBaseline && hasCurrent && baselineBlob.Equals(currentBlob)) {
                continue;
            }

            Hash? baselineHash = hasBaseline ? (Hash?)baselineBlob : null;
            Hash? currentHash = hasCurrent ? (Hash?)currentBlob : null;
            changes.Add(new CommitFileChange(
                path,
                baselineHash,
                await ReadBlobContentAsync(baselineHash),
                currentHash,
                await ReadBlobContentAsync(currentHash)));
        }

        return [.. changes];
    }

    private async Task<CommitHistorySearchResult> GetCommitHistoryAsync(Hash? boundarySha, DateTimeOffset? sinceTimestamp) {
        var head = _repository.Head;
        if (head is null) {
            return boundarySha is null
                ? CommitHistorySearchResult.Complete([])
                : CommitHistorySearchResult.BoundaryNotReached([]);
        }

        var commits = new List<CommitHistoryEntry>();
        Commit? commit = await head.GetHeadCommitAsync(CancellationToken.None);
        while (commit is not null) {
            if (boundarySha is { } boundary && commit.Hash.Equals(boundary)) {
                return CommitHistorySearchResult.BoundaryReached(commit, [.. commits]);
            }

            var parent = await commit.GetPrimaryParentCommitAsync(CancellationToken.None);
            if (sinceTimestamp is null || commit.Committer.Date >= sinceTimestamp.Value) {
                commits.Add(new CommitHistoryEntry(
                    commit,
                    parent,
                    await GetCommitFileNamesAsync(parent, commit)));
            }

            commit = parent;
        }

        return boundarySha is null
            ? CommitHistorySearchResult.Complete([.. commits])
            : CommitHistorySearchResult.BoundaryNotReached([.. commits]);
    }

    private async Task<string[]> GetCommitFileNamesAsync(Commit? parent, Commit commit) {
        var baselineBlobs = parent is null ? new Dictionary<string, Hash>(StringComparer.Ordinal) : await GetCommitBlobHashesAsync(parent);
        var currentBlobs = await GetCommitBlobHashesAsync(commit);
        var paths = new HashSet<string>(baselineBlobs.Keys, StringComparer.Ordinal);
        paths.UnionWith(currentBlobs.Keys);

        return [
            .. paths
                .Where(path => !baselineBlobs.TryGetValue(path, out var baselineBlob) ||
                               !currentBlobs.TryGetValue(path, out var currentBlob) ||
                               !baselineBlob.Equals(currentBlob))
                .OrderBy(path => path, StringComparer.Ordinal)
        ];
    }

    private static bool AreEquivalent(GitBlobContent? baseline, GitBlobContent? current, Hash? baselineHash, Hash? currentHash) {
        if (baseline is null || current is null || baseline.IsTruncated || current.IsTruncated) {
            return false;
        }

        if (baseline.IsBinary || current.IsBinary) {
            return baselineHash is { } baselineValue && currentHash is { } currentValue && baselineValue.Equals(currentValue);
        }

        return string.Equals(NormalizeLineEndings(baseline.Text), NormalizeLineEndings(current.Text), StringComparison.Ordinal);
    }

    private static string? NormalizeLineEndings(string? content) {
        return content?.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static void AddTreeBlobs(IEnumerable<TreeEntry> entries, string directoryPath, Dictionary<string, Hash> blobs) {
        foreach (var entry in entries) {
            var path = string.IsNullOrEmpty(directoryPath) ? entry.Name : $"{directoryPath}/{entry.Name}";
            switch (entry) {
                case TreeBlobEntry blob:
                    blobs[path] = blob.Hash;
                    break;
                case IParentTreeEntry directory:
                    AddTreeBlobs(directory.Children, path, blobs);
                    break;
            }
        }
    }

    private static void AddFiles(Dictionary<string, WorkingDirectoryFile> filesByPath, IEnumerable<WorkingDirectoryFile> files) {
        foreach (var file in files) {
            filesByPath[file.Path] = file;
        }
    }

    private static string GetRepositoryPathArgument(string[] args) {
        if (args.Length == 1 && !args[0].StartsWith("--", StringComparison.Ordinal)) {
            return args[0];
        }

        for (var index = 0; index < args.Length; index++) {
            if (args[index] == "--repository" && index + 1 < args.Length) {
                return args[index + 1];
            }
        }

        throw new ArgumentException("Pass the target repository as an absolute path or with --repository <absolute-path>.");
    }
}
