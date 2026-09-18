using System.Text.Json;
using SafeGitMcp.Tools;

internal static class InteractiveMode
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static async Task RunAsync()
    {
        using var repository = await PromptForRepositoryAsync();
        var tools = new GitRepositoryTools(repository);

        Console.WriteLine("Interactive mode. Enter 'help' for available commands or 'exit' to quit.");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null)
            {
                return;
            }

            var commandLine = CommandLine.TryParse(line);
            if (!commandLine.IsValid)
            {
                Console.WriteLine(commandLine.Error);
                continue;
            }

            if (commandLine.Arguments.Count == 0)
            {
                continue;
            }

            var command = commandLine.Arguments[0];
            var arguments = commandLine.Arguments.Skip(1).ToArray();

            switch (command)
            {
                case "exit":
                case "quit":
                    return;
                case "help":
                    WriteHelp();
                    break;
                case "get_staged_changes" when arguments.Length == 0:
                    WriteResult(await tools.GetStagedChanges());
                    break;
                case "get_commit_by_sha" when arguments.Length == 1:
                    WriteResult(await tools.GetCommitBySha(arguments[0]));
                    break;
                case "get_staged_changes":
                    Console.WriteLine("Usage: get_staged_changes");
                    break;
                case "get_commit_by_sha":
                    Console.WriteLine("Usage: get_commit_by_sha <full-sha1>");
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}. Enter 'help' for available commands.");
                    break;
            }
        }
    }

    private static async Task<GitRepositoryContext> PromptForRepositoryAsync()
    {
        while (true)
        {
            Console.Write("Absolute repository path (or 'exit' to quit): ");
            var repositoryPath = Console.ReadLine();
            if (repositoryPath is null || repositoryPath.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }

            try
            {
                return await GitRepositoryContext.CreateAsync([repositoryPath]);
            }
            catch (Exception exception) when (exception is ArgumentException or DirectoryNotFoundException or IOException)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }

    private static void WriteHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  get_staged_changes");
        Console.WriteLine("  get_commit_by_sha <full-sha1>");
        Console.WriteLine("  exit");
    }

    private static void WriteResult<T>(T result)
    {
        Console.WriteLine(JsonSerializer.Serialize(result, SerializerOptions));
    }
}

internal sealed record CommandLine(bool IsValid, IReadOnlyList<string> Arguments, string? Error)
{
    public static CommandLine TryParse(string input)
    {
        var arguments = new List<string>();
        var currentArgument = new System.Text.StringBuilder();
        var isQuoted = false;
        var isEscaped = false;

        foreach (var character in input)
        {
            if (isEscaped)
            {
                currentArgument.Append(character);
                isEscaped = false;
                continue;
            }

            if (character == '\\')
            {
                isEscaped = true;
                continue;
            }

            if (character == '"')
            {
                isQuoted = !isQuoted;
                continue;
            }

            if (char.IsWhiteSpace(character) && !isQuoted)
            {
                AddCurrentArgument(arguments, currentArgument);
                continue;
            }

            currentArgument.Append(character);
        }

        if (isEscaped)
        {
            currentArgument.Append('\\');
        }

        if (isQuoted)
        {
            return new CommandLine(false, [], "Unterminated quoted argument.");
        }

        AddCurrentArgument(arguments, currentArgument);
        return new CommandLine(true, arguments, null);
    }

    private static void AddCurrentArgument(List<string> arguments, System.Text.StringBuilder currentArgument)
    {
        if (currentArgument.Length > 0)
        {
            arguments.Add(currentArgument.ToString());
            currentArgument.Clear();
        }
    }
}
