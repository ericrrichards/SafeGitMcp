namespace SafeGitMcp.Tools.Responses;

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
