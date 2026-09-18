using GitReader;
using GitReader.Structures;
using System.Text.Json.Serialization;

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
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? BaselineHash => baselineHash?.ToString();
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public GitBlobContent? BaselineContent { get; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? CurrentHash => file.WorkingTreeHash?.ToString();
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public GitBlobContent? CurrentContent { get; }
}
