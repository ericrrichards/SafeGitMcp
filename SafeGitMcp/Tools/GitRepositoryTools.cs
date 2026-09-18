using System.ComponentModel;
using GitReader;
using ModelContextProtocol.Server;
using SafeGitMcp.Tools.Responses;

namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryTools(GitRepositoryContext repository) {
    [McpServerTool(Name = "get_current_changeset", Title = "Get Current Changeset", UseStructuredContent = true)]
    [Description("Gets the complete AI-reviewable pre-commit changeset, regardless of whether files have been added to Git's index.")]
    public async Task<CurrentChangesetResponse> GetCurrentChangeset() {
        return new CurrentChangesetResponse(await repository.GetCurrentChangesetFilesAsync());
    }

    [McpServerTool(Name = "get_commit_by_sha", Title = "Get Commit by SHA", UseStructuredContent = true)]
    [Description("Gets a specific commit by its full SHA from the repository supplied when the server was started.")]
    public async Task<CommitLookupResponse> GetCommitBySha([Description("The complete 40-character hexadecimal SHA-1 commit hash.")] string sha) {
        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return CommitLookupResponse.Failure("The SHA must be a complete 40-character hexadecimal SHA-1 hash.");
        }

        var review = await repository.GetCommitReviewAsync(hash);
        if (review is null) {
            return CommitLookupResponse.Failure("No commit with that SHA exists in this repository.");
        }

        return CommitLookupResponse.FromCommit(
            new CommitDetailsResponse(
                review.Commit,
                review.ComparisonParent,
                review.Changes));
    }
}
