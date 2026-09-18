# SafeGitMcp

`SafeGitMcp` is a read-only stdio MCP server for one local Git working tree. The repository is selected once when the server starts; tools do not accept arbitrary paths, which keeps every request scoped to that repository.

## Available tools

- `get_current_changeset` returns the complete pre-commit unit of work: every path whose working-tree content differs from the local `HEAD` commit appears once, regardless of its index state. Each entry compares the committed baseline to the current working-tree file and includes hashes plus up to 1 MB of UTF-8 content for both sides. Binary or non-UTF-8 files are explicitly marked without attempting to render their contents.
- `get_commit_by_sha` returns a structured lookup response containing `found`, an error when applicable, and complete commit-review data for a full SHA-1 hash: metadata, the primary parent used for comparison, top-level `fileCount` and `fileNames` aggregates, and every added, modified, or deleted file with baseline/current hashes and content snapshots.

The server reads Git repository data through the GitReader NuGet package and never invokes the Git executable. Untracked-file discovery applies both GitReader's common development-file exclusions and the repository-root `.gitignore`.

## Interactive development mode

Start the executable with no arguments to choose a repository and invoke the tools from the console. Results are written as indented structured JSON.

Supported commands are `get_current_changeset`, `get_commit_by_sha <full-sha1>`, `help`, and `exit`.

## Start the server

Supply an **absolute** path to a local Git working tree, either as the sole argument or with `--repository`:

```cmd
dotnet run --project E:\Code\SafeGitMcp\SafeGitMcp -- --repository E:\Code\SomeRepository
```

The server validates the path and resolves it to the working tree root before accepting MCP requests. Logging is written to standard error; standard output is reserved for the MCP stdio protocol.

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
        "--",
        "--repository",
        "E:\\Code\\SomeRepository"
      ]
    }
  }
}
```

## Build

```cmd
dotnet build E:\Code\SafeGitMcp\SafeGitMcp.slnx
```