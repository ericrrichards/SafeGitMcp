using GitReader.Structures;

namespace SafeGitMcp.Tools;

internal sealed class CommitHistorySearchResult {
    private CommitHistorySearchResult(bool reachedBoundary, CommitHistoryEntry[] commits) {
        ReachedBoundary = reachedBoundary;
        Commits = commits;
    }

    private CommitHistorySearchResult(Commit boundaryCommit, CommitHistoryEntry[] commits)
        : this(true, commits) {
        BoundaryCommit = boundaryCommit;
    }

    public bool ReachedBoundary { get; }
    public Commit? BoundaryCommit { get; }
    public CommitHistoryEntry[] Commits { get; }

    public static CommitHistorySearchResult Complete(CommitHistoryEntry[] commits) {
        return new CommitHistorySearchResult(true, commits);
    }

    public static CommitHistorySearchResult BoundaryNotReached(CommitHistoryEntry[] commits) {
        return new CommitHistorySearchResult(false, commits);
    }

    public static CommitHistorySearchResult BoundaryReached(Commit boundaryCommit, CommitHistoryEntry[] commits) {
        return new CommitHistorySearchResult(boundaryCommit, commits);
    }
}
