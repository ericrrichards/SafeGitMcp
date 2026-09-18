using GitReader.Structures;
using SafeGitMcp.Tools;

namespace SafeGitMcp.Tools.Responses;

public sealed class CommitSummaryResponse {
    private readonly Commit _commit;
    private readonly Commit? _comparisonParent;

    private CommitSummaryResponse(Commit commit, IEnumerable<string> fileNames) {
        _commit = commit;
        FileNames = [.. fileNames];
    }

    internal CommitSummaryResponse(CommitHistoryEntry entry)
        : this(entry.Commit, entry.FileNames) {
        _comparisonParent = entry.ComparisonParent;
    }

    public string Sha => _commit.Hash.ToString();
    public string AuthorName => _commit.Author.Name;
    public string? AuthorEmail => _commit.Author.MailAddress;
    public DateTimeOffset AuthorDate => _commit.Author.Date;
    public string CommitterName => _commit.Committer.Name;
    public string? CommitterEmail => _commit.Committer.MailAddress;
    public DateTimeOffset CommitterDate => _commit.Committer.Date;
    public string Subject => _commit.Subject;
    public string Body => _commit.Body;
    public string? ComparisonParentSha => _comparisonParent?.Hash.ToString();
    public int FileCount => FileNames.Length;
    public string[] FileNames { get; }

    internal static CommitSummaryResponse Boundary(Commit commit) {
        return new CommitSummaryResponse(commit, []);
    }
}
