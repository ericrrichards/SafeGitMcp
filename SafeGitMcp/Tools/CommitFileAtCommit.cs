using GitReader;
using GitReader.Structures;

namespace SafeGitMcp.Tools;

internal sealed class CommitFileAtCommit {
    public CommitFileAtCommit(Commit commit, string path, Hash blobHash, GitBlobContent content) {
        Commit = commit;
        Path = path;
        BlobHash = blobHash;
        Content = content;
    }

    public Commit Commit { get; }
    public string Path { get; }
    public Hash BlobHash { get; }
    public GitBlobContent Content { get; }
}
