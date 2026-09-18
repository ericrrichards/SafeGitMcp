namespace SafeGitMcp.Tools;

public sealed record StagedChangesResponse(int Count, StagedFileChange[] Files);

public sealed record StagedFileChange(string Path, string Status, string? IndexHash);
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