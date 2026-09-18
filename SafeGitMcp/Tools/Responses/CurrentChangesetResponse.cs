namespace SafeGitMcp.Tools.Responses;

public sealed class CurrentChangesetResponse {
    public CurrentChangesetResponse(IEnumerable<CurrentChangesetFile> files) {
        Files = [.. files];
    }

    public int Count => Files.Length;
    public CurrentChangesetFile[] Files { get; }
}
