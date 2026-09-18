# SafeGitMcp

`SafeGitMcp` is a read-only stdio MCP server for one local Git working tree. The repository is selected once when the server starts; tools do not accept arbitrary paths, which keeps every request scoped to that repository.

## Available tools

- `get_staged_changes` returns structured staged-file data: a count plus each file's path, status, and index object hash.
- `get_commit_by_sha` returns a structured lookup response containing `found`, an error when applicable, and commit metadata for a full SHA-1 hash.

The server reads Git repository data through the GitReader NuGet package and never invokes the Git executable.

## Interactive development mode

Start the executable with no arguments to choose a repository and invoke the tools from the console. Results are written as indented structured JSON.

Supported commands are `get_staged_changes`, `get_commit_by_sha <full-sha1>`, `help`, and `exit`.

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