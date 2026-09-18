namespace SafeGitMcp.Tools;

public sealed class GitBlobContent {
    private GitBlobContent(bool isBinary, bool isTruncated, int bytesReturned, string? text) {
        IsBinary = isBinary;
        IsTruncated = isTruncated;
        BytesReturned = bytesReturned;
        Text = text;
    }

    public bool IsBinary { get; }
    public bool IsTruncated { get; }
    public int BytesReturned { get; }
    public string? Text { get; }

    public static GitBlobContent Binary(bool isTruncated, int bytesReturned) {
        return new GitBlobContent(true, isTruncated, bytesReturned, null);
    }

    public static GitBlobContent TextContent(bool isTruncated, int bytesReturned, string text) {
        return new GitBlobContent(false, isTruncated, bytesReturned, text);
    }
}