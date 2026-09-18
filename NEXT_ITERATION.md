# Next Iteration Checklist: Shared Core and HTTP MCP Transport

## Preserve the working baseline

- [ ] Keep the existing `SafeGitMcp` stdio server working with Rider before beginning the refactor.
- [ ] Retain the current Rider MCP configuration shape from `C:\Users\eric\AppData\Local\github-copilot\intellij\mcp.json`:
  - [ ] top-level `servers` object
  - [ ] `safe-git` server with `type: stdio`
  - [ ] `dotnet run --project E:\Code\SafeGitMcp\SafeGitMcp`
- [ ] Verify the stdio server can select a repository with `set_repository_root` and serve all existing tools.
- [ ] Capture the expected history regression case: querying from `2026-09-17T21:00:00-04:00` in this repository returns 13 commits, including commits introduced through the merged `main` branch.

## Define the project boundaries

- [ ] Decide on project names and target frameworks.
- [ ] Create a shared class library for repository inspection and tool-independent models, for example `SafeGitMcp.Core`.
- [ ] Keep MCP transport/host startup code out of the shared library.
- [ ] Keep the existing `SafeGitMcp` project as the Rider-oriented stdio host.
- [ ] Create a separate HTTP host project, for example `SafeGitMcp.Http`.
- [ ] Decide whether the HTTP host is intended only for local development or may be exposed to other machines.

## Extract the shared core

- [ ] Move `GitRepositoryContext` into the shared core project.
- [ ] Move repository data models used by both hosts into the shared core project.
- [ ] Move response/data-mapping logic into the core only when it does not depend on MCP attributes or a specific transport.
- [ ] Keep `GitRepositoryTools` and its `[McpServerTool]` attributes in each transport host, or introduce a thin shared application-service layer that both hosts call.
- [ ] Keep repository access disposable and ensure repository replacement cannot dispose an active context.
- [ ] Preserve full commit-graph traversal:
  - [ ] visit all parents of merge commits
  - [ ] return each reachable commit once
  - [ ] return results newest-first
  - [ ] stop expanding a path when it reaches the SHA boundary
  - [ ] continue using the primary parent for a merge commit's file-change comparison
- [ ] Add unit tests for path normalization, timestamp boundaries, root commits, ordinary commits, merge commits, and SHA-boundary traversal.

## Design repository selection for HTTP

- [ ] Decide whether an HTTP process serves exactly one fixed repository or supports selecting repositories dynamically.
- [ ] Prefer one fixed repository per HTTP server process for the first version.
- [ ] If allowing dynamic repository selection over HTTP, make repository context session- or request-scoped; do not share one mutable selected repository between unrelated clients.
- [ ] Define how the fixed repository root is supplied, such as configuration or an environment variable.
- [ ] Validate that the configured root is absolute, exists, and opens as a Git repository before serving requests.
- [ ] Keep `set_repository_root` for the stdio/Rider host even if the HTTP host uses a fixed root.

## Add the HTTP MCP host

- [ ] Select a Model Context Protocol SDK version that supports the ASP.NET Core HTTP transport described in the official SDK documentation.
- [ ] Add the matching ASP.NET Core MCP transport package to the HTTP host project.
- [ ] Use an ASP.NET Core `WebApplication` host for HTTP mode.
- [ ] Register the MCP server, HTTP transport, and tool types through dependency injection.
- [ ] Map the MCP endpoint using the SDK-supported endpoint mapping API.
- [ ] Choose and document the endpoint path, such as `/mcp`.
- [ ] Configure the listen address and port explicitly.
- [ ] Keep HTTP hosting separate from `--interactive` mode.

## Secure the HTTP host

- [ ] Bind to loopback (`127.0.0.1` or `localhost`) by default.
- [ ] Do not expose the server on a network interface by default.
- [ ] Require authentication before permitting non-loopback access.
- [ ] Document that commit and file-content responses can contain sensitive local source data.
- [ ] If dynamic repository roots are supported, restrict roots to approved directories.
- [ ] Confirm logging does not write MCP protocol payloads or file content unintentionally.

## Validate both hosts

- [ ] Build the complete solution after stopping any running server that locks build outputs.
- [ ] Run automated tests for the shared core.
- [ ] Verify the existing Rider stdio configuration still connects and works.
- [ ] Verify the stdio host supports repository replacement without disposing a repository during an active request.
- [ ] Verify HTTP initialization, tool discovery, and each existing tool call.
- [ ] Verify HTTP handling for root, normal, and merge commits.
- [ ] Verify timestamp boundary behavior with explicit offsets, including `2026-09-17T21:00:00-04:00`.
- [ ] Verify a new HTTP server process begins with no selected repository when dynamic selection is enabled.
- [ ] Verify concurrent HTTP requests cannot observe another client's repository selection.

## Update documentation and packaging

- [ ] Update the solution file with the new projects.
- [ ] Update `README.md` to distinguish stdio/Rider, interactive development, and HTTP hosting.
- [ ] Keep the Rider stdio example based on the configuration that is known to work locally.
- [ ] Add a separate HTTP configuration example only after validating it with the intended MCP client.
- [ ] Document repository-selection behavior separately for stdio and HTTP.
- [ ] Update package metadata and tags if the HTTP project is packaged or published.
- [ ] Review the committed-file list to avoid adding IDE-local metadata or build output.
