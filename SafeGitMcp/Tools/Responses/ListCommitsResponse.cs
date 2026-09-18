namespace SafeGitMcp.Tools.Responses;

public sealed class ListCommitsResponse {
    private readonly CommitSummaryResponse? _boundary;
    private readonly string? _requestedSha;
    private readonly DateTimeOffset? _requestedTimestamp;

    private ListCommitsResponse(CommitSummaryResponse[] commits) {
        Commits = commits;
    }

    private ListCommitsResponse(CommitSummaryResponse boundary, CommitSummaryResponse[] commits)
        : this(commits) {
        _boundary = boundary;
    }

    private ListCommitsResponse(DateTimeOffset requestedTimestamp, CommitSummaryResponse[] commits)
        : this(commits) {
        _requestedTimestamp = requestedTimestamp;
    }

    private ListCommitsResponse(string requestedSha, string error)
        : this([]) {
        _requestedSha = requestedSha;
        Error = error;
    }

    public string? SinceSha => _boundary?.Sha ?? _requestedSha;
    public DateTimeOffset? SinceTimestamp => _boundary?.CommitterDate ?? _requestedTimestamp;
    public string? Error { get; }
    public int Count => Commits.Length;
    public CommitSummaryResponse[] Commits { get; }

    public static ListCommitsResponse ForSha(CommitSummaryResponse boundary, IEnumerable<CommitSummaryResponse> commits) {
        return new ListCommitsResponse(boundary, [.. commits]);
    }

    public static ListCommitsResponse ForTimestamp(DateTimeOffset timestamp, IEnumerable<CommitSummaryResponse> commits) {
        CommitSummaryResponse[] summaries = [.. commits];
        return summaries.FirstOrDefault() is { } firstCommit
            ? new ListCommitsResponse(firstCommit, summaries)
            : new ListCommitsResponse(timestamp, summaries);
    }

    public static ListCommitsResponse FailureSinceSha(string sha, string error) {
        return new ListCommitsResponse(sha, error);
    }
}
