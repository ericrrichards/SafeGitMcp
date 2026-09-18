using GitReader.Structures;

namespace SafeGitMcp.Tools;

internal sealed class CommitHistoryEntry {
    public CommitHistoryEntry(Commit commit, Commit? comparisonParent, string[] fileNames) {
        Commit = commit;
        ComparisonParent = comparisonParent;
        FileNames = fileNames;
    }

    public Commit Commit { get; }
    public Commit? ComparisonParent { get; }
    public string[] FileNames { get; }
}
