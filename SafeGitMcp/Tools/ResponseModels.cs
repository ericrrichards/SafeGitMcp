namespace SafeGitMcp.Tools;

public sealed record CurrentChangesetResponse(int Count, CurrentChangesetFile[] Files);
public sealed record CurrentChangesetFile(
    string Path,
    string Status,
    string? BaselineHash,
    GitBlobContent? BaselineContent,
    string? CurrentHash,
    GitBlobContent? CurrentContent);
public sealed record GitBlobContent(bool IsBinary, bool IsTruncated, int BytesReturned, string? Text);
public sealed record CommitLookupResponse(bool Found, string? Error, CommitDetailsResponse? Commit);
public sealed record CommitDetailsResponse(
    string Sha,
    string AuthorName,
    string? AuthorEmail,
    DateTimeOffset AuthorDate,
    string CommitterName,
    string? CommitterEmail,
    DateTimeOffset CommitterDate,
    string Subject,
    string Body);
