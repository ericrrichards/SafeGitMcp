# SafeGitMcp

`SafeGitMcp` is a read-only stdio MCP server for one local Git working tree at a time. Start by calling `set_repository_root` with an absolute repository path; it scopes every subsequent request to that selected repository.

## Available tools

- `set_repository_root` sets or replaces the absolute local Git repository root used by every other tool. It must be called before querying repository data.
- `get_current_changeset` returns the complete pre-commit unit of work: every path whose working-tree content differs from the local `HEAD` commit appears once, regardless of its index state. Each entry compares the committed baseline to the current working-tree file and includes hashes plus up to 1 MB of UTF-8 content for both sides. Binary or non-UTF-8 files are explicitly marked without attempting to render their contents.
- `get_commit_by_sha` returns a structured lookup response containing `found`, an error when applicable, and complete commit-review data for a full SHA-1 hash: metadata, the primary parent used for comparison, top-level `fileCount` and `fileNames` aggregates, and every added, modified, or deleted file with baseline/current hashes and content snapshots.
- `get_file_at_commit` returns a structured lookup response for one repository-relative path at a full SHA-1 commit, including the commit SHA, blob hash, and up to 1 MB of UTF-8 content. Backslashes and a leading `./` are accepted in the path.
- `get_uncommitted_file_diff` returns one repository-relative file from the current uncommitted changeset, with its local `HEAD` baseline and current working-tree hashes and content snapshots. Backslashes and a leading `./` are accepted in the path.
- `list_commits_since_sha` walks every parent path reachable from `HEAD` and returns metadata-only summaries until each path reaches the supplied full SHA-1 boundary, which is excluded from the results. This includes commits introduced through a merge commit's non-primary parents. Its response reports the resolved boundary SHA, result count, and summaries with each commit's changed-file names but no file content.
- `list_commits_since_timestamp` lists the same metadata-only summaries for every commit reachable from `HEAD` whose committer timestamp is on or after the supplied ISO 8601 timestamp, including commits on merged branches. Its response reports the newest returned commit's timestamp and result count.

History results are ordered newest-first and include each reachable commit only once. File-change summaries for an individual merge commit continue to compare that commit with its primary parent.

The server reads Git repository data through the GitReader NuGet package and never invokes the Git executable. Untracked-file discovery applies both GitReader's common development-file exclusions and the repository-root `.gitignore`.

## Build

From the repository root, build the solution:

```cmd
dotnet build SafeGitMcp.slnx
```

## Connect an MCP client

SafeGitMcp uses standard input and output for the MCP protocol. Configure your MCP client to launch it; do not run the stdio server in a terminal expecting an interactive prompt. Logging is written to standard error.

Rider's active MCP configuration uses the following stdio entry to run this checkout:

```json
{
  "servers": {
    "safe-git": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "E:\\Code\\SafeGitMcp\\SafeGitMcp"
      ]
    }
  }
}
```

After the client connects, call `set_repository_root` with an absolute working-tree path before using repository-dependent tools. The selected repository exists only for that server process; reconnecting or restarting the server requires selecting it again.

## Interactive development mode

Interactive mode is for local development and manual inspection. It is separate from the MCP stdio server:

```cmd
dotnet run --project SafeGitMcp\SafeGitMcp.csproj -- --interactive
```

Enter an absolute repository path, or `.` to locate the nearest parent directory containing `.git`. Results are written as indented structured JSON. Use `help` to display the supported commands; timestamps accept standard forms plus values such as `2026-09-17-20:00` and `2026-09-17 20:00`.