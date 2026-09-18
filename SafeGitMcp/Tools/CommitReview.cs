using GitReader.Structures;

namespace SafeGitMcp.Tools;

internal sealed class CommitReview {
    public CommitReview(Commit commit, Commit? comparisonParent, CommitFileChange[] changes) {
        Commit = commit;
        ComparisonParent = comparisonParent;
        Changes = changes;
    }

    public Commit Commit { get; }
    public Commit? ComparisonParent { get; }
    public CommitFileChange[] Changes { get; }
}
