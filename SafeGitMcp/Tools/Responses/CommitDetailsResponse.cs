using GitReader.Structures;

namespace SafeGitMcp.Tools.Responses;

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
