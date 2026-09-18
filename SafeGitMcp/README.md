# SafeGitMcp

`SafeGitMcp` is a read-only stdio MCP server for one local Git working tree at a time. Start by calling `set_repository_root` with an absolute repository path; it scopes every subsequent request to that selected repository.

## Available tools

- `set_repository_root` sets or replaces the absolute local Git repository root used by every other tool. It must be called before querying repository data.
- `get_current_changeset` returns the complete pre-commit unit of work: every path whose working-tree content differs from the local `HEAD` commit appears once, regardless of its index state. Each entry compares the committed baseline to the current working-tree file and includes hashes plus up to 1 MB of UTF-8 content for both sides. Binary or non-UTF-8 files are explicitly marked without attempting to render their contents.
- `get_commit_by_sha` returns a structured lookup response containing `found`, an error when applicable, and complete commit-review data for a full SHA-1 hash: metadata, the primary parent used for comparison, top-level `fileCount` and `fileNames` aggregates, and every added, modified, or deleted file with baseline/current hashes and content snapshots.
- `get_file_at_commit` returns a structured lookup response for one repository-relative path at a full SHA-1 commit, including the commit SHA, blob hash, and up to 1 MB of UTF-8 content. Backslashes and a leading `./` are accepted in the path.
- `get_uncommitted_file_diff` returns one repository-relative file from the current uncommitted changeset, with its local `HEAD` baseline and current working-tree hashes and content snapshots. Backslashes and a leading `./` are accepted in the path.
- `list_commits_since_sha` lists metadata-only summaries from `HEAD` back to, but excluding, a full SHA-1 commit on the primary-parent history. Its common response includes the supplied SHA, result count, and summaries with each commit's changed-file names but no file content.
- `list_commits_since_timestamp` lists the same metadata-only summaries for primary-parent-history commits whose committer timestamp is on or after the supplied ISO 8601 timestamp. Its common response includes the supplied timestamp and result count.

The server reads Git repository data through the GitReader NuGet package and never invokes the Git executable. Untracked-file discovery applies both GitReader's common development-file exclusions and the repository-root `.gitignore`.

## Interactive development mode

Start the executable with `--interactive` to choose a repository and invoke the tools from the console. Enter an absolute repository path, or `.` to search from the current directory upward for the nearest `.git` folder. Results are written as indented structured JSON.

Supported commands are `set_repository_root <absolute-path>`, `get_current_changeset`, `get_commit_by_sha <full-sha1>`, `get_file_at_commit <full-sha1> <repository-relative-path>`, `get_uncommitted_file_diff <repository-relative-path>`, `list_commits_since_sha <full-sha1>`, `list_commits_since_timestamp <timestamp>`, `help`, and `exit`. Interactive mode accepts standard timestamps plus common forms such as `2026-09-17-20:00` and `2026-09-17 20:00`.

## Start the server

Start the server without a repository argument, then call `set_repository_root` in chat:

```cmd
dotnet run --project E:\Code\SafeGitMcp\SafeGitMcp
```

The repository-root tool validates the path and opens the working tree before replacing the current repository. Logging is written to standard error; standard output is reserved for the MCP stdio protocol.

## MCP configuration example

```json
{
  "servers": {
    "safe-git": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "E:\\Code\\SafeGitMcp\\SafeGitMcp",
        "--"
      ]
    }
  }
}
```

## Build

```cmd
dotnet build E:\Code\SafeGitMcp\SafeGitMcp.slnx
```