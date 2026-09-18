using GitReader;
using GitReader.Structures;

namespace SafeGitMcp.Tools;

public sealed class CurrentChangesetFile {
    private readonly WorkingDirectoryFile file;
    private readonly Hash? baselineHash;

    public CurrentChangesetFile(
        WorkingDirectoryFile file,
        Hash? baselineHash,
        GitBlobContent? baselineContent,
        GitBlobContent? currentContent) {
        this.file = file;
        this.baselineHash = baselineHash;
        BaselineContent = baselineContent;
        CurrentContent = currentContent;
    }

    public string Path => file.Path;
    public string Status => file.Status.ToString();
    public string? BaselineHash => baselineHash?.ToString();
    public GitBlobContent? BaselineContent { get; }
    public string? CurrentHash => file.WorkingTreeHash?.ToString();
    public GitBlobContent? CurrentContent { get; }
}
