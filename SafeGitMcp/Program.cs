using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeGitMcp.Tools;

if (args.Length == 0) {
    await InteractiveMode.RunAsync();
    return;
}

var repository = await GitRepositoryContext.CreateAsync(args);

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GitRepositoryTools>();

builder.Services.AddSingleton(repository);

await builder.Build().RunAsync();
