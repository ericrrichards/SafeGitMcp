using SafeGitMcp.Tools;

namespace SafeGitMcp.Tools.Responses;

public sealed class CommitFileDetailsResponse {
    internal CommitFileDetailsResponse(CommitFileAtCommit file) {
        _file = file;
    }

    private readonly CommitFileAtCommit _file;

    public string CommitSha => _file.Commit.Hash.ToString();
    public string Path => _file.Path;
    public string BlobHash => _file.BlobHash.ToString();
    public GitBlobContent Content => _file.Content;
}
