using GitReader;

namespace SafeGitMcp.Tools;

public sealed class CommitFileChange {
    private readonly Hash? baselineHash;
    private readonly Hash? currentHash;

    public CommitFileChange(
        string path,
        Hash? baselineHash,
        GitBlobContent? baselineContent,
        Hash? currentHash,
        GitBlobContent? currentContent) {
        if (baselineHash is null && currentHash is null) {
            throw new ArgumentException("A commit file change must have a baseline or current blob.");
        }

        Path = path;
        this.baselineHash = baselineHash;
        BaselineContent = baselineContent;
        this.currentHash = currentHash;
        CurrentContent = currentContent;
    }

    public string Path { get; }
    public string ChangeType => (baselineHash, currentHash) switch {
        (null, not null) => "Added",
        (not null, null) => "Deleted",
        _ => "Modified"
    };
    public string? BaselineHash => baselineHash?.ToString();
    public GitBlobContent? BaselineContent { get; }
    public string? CurrentHash => currentHash?.ToString();
    public GitBlobContent? CurrentContent { get; }
}
