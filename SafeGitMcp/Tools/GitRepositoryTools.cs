using System.ComponentModel;
using GitReader;
using GitReader.Structures;
using ModelContextProtocol.Server;

namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryTools(GitRepositoryContext repository) {
    [McpServerTool(Name = "get_staged_changes", Title = "Get Staged Changes", UseStructuredContent = true)]
    [Description("Gets the files currently staged in the repository supplied when the server was started.")]
    public async Task<StagedChangesResponse> GetStagedChanges() {
        var status = await repository.Repository.GetWorkingDirectoryStatusAsync(CancellationToken.None);
        var files = status.StagedFiles
            .Select(file => new StagedFileChange(file.Path, file.Status.ToString(), file.IndexHash?.ToString()))
            .ToArray();

        return new StagedChangesResponse(files.Length, files);
    }

    [McpServerTool(Name = "get_commit_by_sha", Title = "Get Commit by SHA", UseStructuredContent = true)]
    [Description("Gets a specific commit by its full SHA from the repository supplied when the server was started.")]
    public async Task<CommitLookupResponse> GetCommitBySha([Description("The complete 40-character hexadecimal SHA-1 commit hash.")] string sha) {
        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return new CommitLookupResponse(false, "The SHA must be a complete 40-character hexadecimal SHA-1 hash.", null);
        }

        var commit = await repository.Repository.GetCommitAsync(hash, CancellationToken.None);
        if (commit is null) {
            return new CommitLookupResponse(false, "No commit with that SHA exists in this repository.", null);
        }

        return new CommitLookupResponse(
            true,
            null,
            new CommitDetailsResponse(
                commit.Hash.ToString(),
                commit.Author.Name,
                commit.Author.MailAddress,
                commit.Author.Date,
                commit.Committer.Name,
                commit.Committer.MailAddress,
                commit.Committer.Date,
                commit.Subject,
                commit.Body));
    }
}

