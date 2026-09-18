using System.ComponentModel;
using GitReader;
using ModelContextProtocol.Server;
using SafeGitMcp.Tools.Responses;

namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryTools(GitRepositoryContextManager repositories) {
    private const string RepositoryNotConfiguredError = "No repository root is configured. Call set_repository_root with an absolute Git repository path first.";

    [McpServerTool(Name = "set_repository_root", Title = "Set Repository Root", UseStructuredContent = true)]
    [Description("Sets the absolute Git repository root used by all subsequent SafeGitMcp tool calls.")]
    public async Task<SetRepositoryRootResponse> SetRepositoryRoot([Description("The absolute path to the local Git repository root.")] string repositoryRoot) {
        try {
            return SetRepositoryRootResponse.Configured(await repositories.SetRepositoryRootAsync(repositoryRoot));
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            return SetRepositoryRootResponse.Failure(exception);
        }
    }

    [McpServerTool(Name = "get_current_changeset", Title = "Get Current Changeset", UseStructuredContent = true)]
    [Description("Gets the complete AI-reviewable pre-commit changeset, regardless of whether files have been added to Git's index.")]
    public async Task<CurrentChangesetResponse> GetCurrentChangeset() {
        if (!repositories.IsConfigured) {
            return CurrentChangesetResponse.Failure(RepositoryNotConfiguredError);
        }

        return new CurrentChangesetResponse(await repositories.UseAsync(repository => repository.GetCurrentChangesetFilesAsync()));
    }

    [McpServerTool(Name = "get_commit_by_sha", Title = "Get Commit by SHA", UseStructuredContent = true)]
    [Description("Gets a specific commit by its full SHA from the repository supplied when the server was started.")]
    public async Task<CommitLookupResponse> GetCommitBySha([Description("The complete 40-character hexadecimal SHA-1 commit hash.")] string sha) {
        if (!repositories.IsConfigured) {
            return CommitLookupResponse.Failure(RepositoryNotConfiguredError);
        }

        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return CommitLookupResponse.Failure("The SHA must be a complete 40-character hexadecimal SHA-1 hash.");
        }

        var review = await repositories.UseAsync(repository => repository.GetCommitReviewAsync(hash));
        if (review is null) {
            return CommitLookupResponse.Failure("No commit with that SHA exists in this repository.");
        }

        return CommitLookupResponse.FromCommit(
            new CommitDetailsResponse(
                review.Commit,
                review.ComparisonParent,
                review.Changes));
    }

    [McpServerTool(Name = "get_file_at_commit", Title = "Get File at Commit", UseStructuredContent = true)]
    [Description("Gets a repository-relative file's blob hash and content at a specific full SHA-1 commit.")]
    public async Task<CommitFileLookupResponse> GetFileAtCommit(
        [Description("The complete 40-character hexadecimal SHA-1 commit hash.")]
        string sha,
        [Description("The repository-relative path of the file. Forward slashes and backslashes are accepted.")]
        string path) {
        if (!repositories.IsConfigured) {
            return CommitFileLookupResponse.Failure(RepositoryNotConfiguredError);
        }

        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return CommitFileLookupResponse.Failure("The SHA must be a complete 40-character hexadecimal SHA-1 hash.");
        }

        if (!TryNormalizeRepositoryPath(path, out var normalizedPath)) {
            return CommitFileLookupResponse.Failure("The file path must be relative to the repository root.");
        }

        var file = await repositories.UseAsync(repository => repository.GetCommitFileAsync(hash, normalizedPath));
        if (file is null) {
            return CommitFileLookupResponse.Failure("No file with that path exists at the specified commit.");
        }

        return CommitFileLookupResponse.FromFile(new CommitFileDetailsResponse(file));
    }

    [McpServerTool(Name = "get_uncommitted_file_diff", Title = "Get Uncommitted File Diff", UseStructuredContent = true)]
    [Description("Gets one uncommitted file's current working-tree content against its version in local HEAD.")]
    public async Task<UncommittedFileDiffResponse> GetUncommittedFileDiff(
        [Description("The repository-relative path of the uncommitted file. Forward slashes and backslashes are accepted.")]
        string path) {
        if (!repositories.IsConfigured) {
            return UncommittedFileDiffResponse.Failure(RepositoryNotConfiguredError);
        }

        if (!TryNormalizeRepositoryPath(path, out var normalizedPath)) {
            return UncommittedFileDiffResponse.Failure("The file path must be relative to the repository root.");
        }

        var file = await repositories.UseAsync(repository => repository.GetCurrentChangesetFileAsync(normalizedPath));
        if (file is null) {
            return UncommittedFileDiffResponse.Failure("The file is not part of the current uncommitted changeset.");
        }

        return UncommittedFileDiffResponse.FromFile(file);
    }

    [McpServerTool(Name = "list_commits_since_sha", Title = "List Commits Since SHA", UseStructuredContent = true)]
    [Description("Lists metadata for commits from HEAD back to, but excluding, the specified full SHA-1 commit.")]
    public async Task<ListCommitsResponse> ListCommitsSinceSha([Description("The complete 40-character hexadecimal SHA-1 commit hash to exclude from the results.")] string sha) {
        if (!repositories.IsConfigured) {
            return ListCommitsResponse.Failure(RepositoryNotConfiguredError);
        }

        if (string.IsNullOrWhiteSpace(sha) || !Hash.TryParse(sha.Trim(), out var hash)) {
            return ListCommitsResponse.FailureSinceSha(sha, "The SHA must be a complete 40-character hexadecimal SHA-1 hash.");
        }

        var history = await repositories.UseAsync(repository => repository.GetCommitsSinceShaAsync(hash));
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
        if (!repositories.IsConfigured) {
            return ListCommitsResponse.Failure(RepositoryNotConfiguredError);
        }

        var commits = await repositories.UseAsync(repository => repository.GetCommitsSinceTimestampAsync(timestamp));
        return ListCommitsResponse.ForTimestamp(
            timestamp,
            [
                .. commits.Select(commit => new CommitSummaryResponse(commit))
            ]);
    }

    private static bool TryNormalizeRepositoryPath(string path, out string normalizedPath) {
        normalizedPath = path.Trim().Replace('\\', '/');
        while (normalizedPath.StartsWith("./", StringComparison.Ordinal)) {
            normalizedPath = normalizedPath[2..];
        }

        return !string.IsNullOrWhiteSpace(normalizedPath) &&
               normalizedPath != ".." &&
               !normalizedPath.StartsWith("../", StringComparison.Ordinal) &&
               !normalizedPath.StartsWith("/", StringComparison.Ordinal);
    }
}
