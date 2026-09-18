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

    [McpServerTool(Name = "list_commits_since_sha", Title = "List Commits Since SHA", UseStructuredContent = true)]
    [Description("Lists metadata for commits from HEAD back to, but excluding, the specified full SHA-1 commit.")]
    public async Task<ListCommitsResponse> ListCommitsSinceSha([Description("The complete 40-character hexadecimal SHA-1 commit hash to exclude from the results.")] string sha) {
        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return ListCommitsResponse.FailureSinceSha(sha, "The SHA must be a complete 40-character hexadecimal SHA-1 hash.");
        }

        var history = await repository.GetCommitsSinceShaAsync(hash);
        if (!history.ReachedBoundary || history.BoundaryCommit is null) {
            return ListCommitsResponse.FailureSinceSha(sha, "The SHA is not reachable from the repository's HEAD commit through its primary-parent history.");
        }

        return ListCommitsResponse.ForSha(
            CommitSummaryResponse.Boundary(history.BoundaryCommit),
            [
                .. history.Commits.Select(commit => new CommitSummaryResponse(commit))
            ]);
    }

    [McpServerTool(Name = "list_commits_since_timestamp", Title = "List Commits Since Timestamp", UseStructuredContent = true)]
    [Description("Lists metadata for commits from HEAD whose committer timestamp is on or after the specified timestamp.")]
    public async Task<ListCommitsResponse> ListCommitsSinceTimestamp([Description("An ISO 8601 timestamp. Commits with a committer timestamp on or after this value are included.")] DateTimeOffset timestamp) {
        var commits = await repository.GetCommitsSinceTimestampAsync(timestamp);
        return ListCommitsResponse.ForTimestamp(
            timestamp,
            [
                .. commits.Select(commit => new CommitSummaryResponse(commit))
            ]);
    }
}
