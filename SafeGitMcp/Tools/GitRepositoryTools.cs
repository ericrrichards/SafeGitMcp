using System.ComponentModel;
using System.Text;
using GitReader;
using GitReader.Structures;
using ModelContextProtocol.Server;

namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryTools(GitRepositoryContext repository) {
    private const int MaximumBlobBytes = 1_000_000;

    [McpServerTool(Name = "get_current_changeset", Title = "Get Current Changeset", UseStructuredContent = true)]
    [Description("Gets the complete AI-reviewable pre-commit changeset, regardless of whether files have been added to Git's index.")]
    public async Task<CurrentChangesetResponse> GetCurrentChangeset() {
        var ignoreFilter = await GetChangesetIgnoreFilterAsync();
        var status = await repository.Repository.GetWorkingDirectoryStatusAsync(ignoreFilter, CancellationToken.None);
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

            files.Add(new CurrentChangesetFile(
                file.Path,
                file.Status.ToString(),
                baselineHash?.ToString(),
                baselineContent,
                file.WorkingTreeHash?.ToString(),
                currentContent));
        }

        return new CurrentChangesetResponse(files.Count, [.. files]);
    }

    [McpServerTool(Name = "get_commit_by_sha", Title = "Get Commit by SHA", UseStructuredContent = true)]
    [Description("Gets a specific commit by its full SHA from the repository supplied when the server was started.")]
    public async Task<CommitLookupResponse> GetCommitBySha([Description("The complete 40-character hexadecimal SHA-1 commit hash.")] string sha) {
        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return new CommitLookupResponse(false, "The SHA must be a complete 40-character hexadecimal SHA-1 hash.", null);
        }

        var commit = await repository.Repository.GetCommitAsync(hash, CancellationToken.None);
        if (commit is null) {
            return new CommitLookupResponse(false, "No commit with that SHA exists in this repository.", null);
        }

        return new CommitLookupResponse(
            true,
            null,
            new CommitDetailsResponse(
                commit.Hash.ToString(),
                commit.Author.Name,
                commit.Author.MailAddress,
                commit.Author.Date,
                commit.Committer.Name,
                commit.Committer.MailAddress,
                commit.Committer.Date,
                commit.Subject,
                commit.Body));
    }

    private async Task<GitBlobContent?> ReadBlobContentAsync(Hash? hash) {
        if (hash is null) {
            return null;
        }

        using var result = await repository.Repository.OpenRawObjectStreamAsync(hash.Value, CancellationToken.None);
        if (result.Type != ObjectTypes.Blob) {
            return null;
        }

        return await ReadContentAsync(result.Stream);
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
            return new GitBlobContent(true, isTruncated, content.Length, null);
        }

        try {
            return new GitBlobContent(false, isTruncated, content.Length, new UTF8Encoding(false, true).GetString(content));
        } catch (DecoderFallbackException) {
            return new GitBlobContent(true, isTruncated, content.Length, null);
        }
    }

    private async Task<GitBlobContent?> ReadWorkingTreeContentAsync(string relativePath) {
        var filePath = Path.GetFullPath(Path.Combine(repository.WorkingDirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var pathFromRepository = Path.GetRelativePath(repository.WorkingDirectoryPath, filePath);
        if (Path.IsPathFullyQualified(pathFromRepository) || pathFromRepository == ".." || pathFromRepository.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || !File.Exists(filePath)) {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await ReadContentAsync(stream);
    }

    private async Task<GlobFilter> GetChangesetIgnoreFilterAsync() {
        var commonFilter = Glob.GetCommonIgnoreFilter();
        var gitIgnorePath = Path.Combine(repository.WorkingDirectoryPath, ".gitignore");
        if (!File.Exists(gitIgnorePath)) {
            return commonFilter;
        }

        await using var stream = File.OpenRead(gitIgnorePath);
        var repositoryFilter = await Glob.CreateExcludeFilterFromGitignoreAsync(stream, CancellationToken.None);
        return Glob.Combine(commonFilter, repositoryFilter);
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

    private async Task<Dictionary<string, Hash>> GetHeadBlobHashesAsync() {
        var head = repository.Repository.Head;
        if (head is null) {
            return [];
        }

        var headCommit = await head.GetHeadCommitAsync(CancellationToken.None);
        var root = await headCommit.GetTreeRootAsync(CancellationToken.None);
        var blobs = new Dictionary<string, Hash>(StringComparer.Ordinal);
        AddTreeBlobs(root.Children, string.Empty, blobs);
        return blobs;
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
}
