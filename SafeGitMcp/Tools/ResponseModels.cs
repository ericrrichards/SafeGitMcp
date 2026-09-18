using GitReader;
using GitReader.Structures;

namespace SafeGitMcp.Tools;

public sealed class CurrentChangesetResponse {
    public CurrentChangesetResponse(IEnumerable<CurrentChangesetFile> files) {
        Files = [.. files];
    }

    public int Count => Files.Length;
    public CurrentChangesetFile[] Files { get; }
}
public sealed class CurrentChangesetFile {
    private readonly WorkingDirectoryFile file;
    private readonly Hash? baselineHash;

    public CurrentChangesetFile(
        WorkingDirectoryFile file,
        Hash? baselineHash,
        GitBlobContent? baselineContent,
        GitBlobContent? currentContent) {
        this.file = file;
        this.baselineHash = baselineHash;
        BaselineContent = baselineContent;
        CurrentContent = currentContent;
    }

    public string Path => file.Path;
    public string Status => file.Status.ToString();
    public string? BaselineHash => baselineHash?.ToString();
    public GitBlobContent? BaselineContent { get; }
    public string? CurrentHash => file.WorkingTreeHash?.ToString();
    public GitBlobContent? CurrentContent { get; }
}
public sealed class GitBlobContent {
    private GitBlobContent(bool isBinary, bool isTruncated, int bytesReturned, string? text) {
        IsBinary = isBinary;
        IsTruncated = isTruncated;
        BytesReturned = bytesReturned;
        Text = text;
    }

    public bool IsBinary { get; }
    public bool IsTruncated { get; }
    public int BytesReturned { get; }
    public string? Text { get; }

    public static GitBlobContent Binary(bool isTruncated, int bytesReturned) {
        return new GitBlobContent(true, isTruncated, bytesReturned, null);
    }

    public static GitBlobContent TextContent(bool isTruncated, int bytesReturned, string text) {
        return new GitBlobContent(false, isTruncated, bytesReturned, text);
    }
}
public sealed class CommitLookupResponse {
    private CommitLookupResponse(string? error, CommitDetailsResponse? commit) {
        Error = error;
        Commit = commit;
    }

    public bool Found => Commit is not null;
    public string? Error { get; }
    public CommitDetailsResponse? Commit { get; }

    public static CommitLookupResponse Failure(string error) {
        return new CommitLookupResponse(error, null);
    }

    public static CommitLookupResponse FromCommit(CommitDetailsResponse commit) {
        return new CommitLookupResponse(null, commit);
    }
}
public sealed class CommitDetailsResponse {
    private readonly Commit commit;
    private readonly Commit? comparisonParent;

    public CommitDetailsResponse(Commit commit, Commit? comparisonParent, CommitFileChange[] changes) {
        this.commit = commit;
        this.comparisonParent = comparisonParent;
        Changes = changes;
    }

    public string Sha => commit.Hash.ToString();
    public string AuthorName => commit.Author.Name;
    public string? AuthorEmail => commit.Author.MailAddress;
    public DateTimeOffset AuthorDate => commit.Author.Date;
    public string CommitterName => commit.Committer.Name;
    public string? CommitterEmail => commit.Committer.MailAddress;
    public DateTimeOffset CommitterDate => commit.Committer.Date;
    public string Subject => commit.Subject;
    public string Body => commit.Body;
    public string? ComparisonParentSha => comparisonParent?.Hash.ToString();
    public CommitFileChange[] Changes { get; }
    public int FileCount => Changes.Length;
    public string[] FileNames => [.. Changes.Select(change => change.Path)];
}
public sealed class CommitFileChange {
    private readonly Hash? baselineHash;
    private readonly Hash? currentHash;

    public CommitFileChange(
        string path,
        Hash? baselineHash,
        GitBlobContent? baselineContent,
        Hash? currentHash,
        GitBlobContent? currentContent) {
        if (baselineHash is null && currentHash is null) {
            throw new ArgumentException("A commit file change must have a baseline or current blob.");
        }

        Path = path;
        this.baselineHash = baselineHash;
        BaselineContent = baselineContent;
        this.currentHash = currentHash;
        CurrentContent = currentContent;
    }

    public string Path { get; }
    public string ChangeType => (baselineHash, currentHash) switch {
        (null, not null) => "Added",
        (not null, null) => "Deleted",
        _ => "Modified"
    };
    public string? BaselineHash => baselineHash?.ToString();
    public GitBlobContent? BaselineContent { get; }
    public string? CurrentHash => currentHash?.ToString();
    public GitBlobContent? CurrentContent { get; }
}
