namespace SafeGitMcp.Tools.Responses;

public sealed class CurrentChangesetResponse {
    private CurrentChangesetResponse(string error)
        : this([]) {
        Error = error;
    }

    public CurrentChangesetResponse(IEnumerable<CurrentChangesetFile> files) {
        Files = [.. files];
    }

    public string? Error { get; }
    public int Count => Files.Length;
    public CurrentChangesetFile[] Files { get; }

    public static CurrentChangesetResponse Failure(string error) {
        return new CurrentChangesetResponse(error);
    }
}
