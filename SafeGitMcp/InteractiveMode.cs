using System.Text.Json;
using SafeGitMcp.Tools;

internal static class InteractiveMode {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true
    };

    public static async Task RunAsync() {
        using var repository = await PromptForRepositoryAsync();
        var tools = new GitRepositoryTools(repository);

        Console.WriteLine("Interactive mode. Enter 'help' for available commands or 'exit' to quit.");

        while (true) {
            Console.Write("> ");
            Console.Out.Flush();
            var line = Console.ReadLine();
            if (line is null) {
                return;
            }

            var commandLine = CommandLine.TryParse(line);
            if (!commandLine.IsValid) {
                Console.WriteLine(commandLine.Error);
                continue;
            }

            if (commandLine.Arguments.Count == 0) {
                continue;
            }

            var command = commandLine.Arguments[0];
            var arguments = commandLine.Arguments.Skip(1).ToArray();

            switch (command) {
                case "exit":
                case "quit":
                    return;
                case "help":
                    WriteHelp();
                    break;
                case "get_current_changeset" when arguments.Length == 0:
                    WriteResult(await tools.GetCurrentChangeset());
                    break;
                case "get_commit_by_sha" when arguments.Length == 1:
                    WriteResult(await tools.GetCommitBySha(arguments[0]));
                    break;
                case "get_file_at_commit" when arguments.Length == 2:
                    WriteResult(await tools.GetFileAtCommit(arguments[0], arguments[1]));
                    break;
                case "get_uncommitted_file_diff" when arguments.Length == 1:
                    WriteResult(await tools.GetUncommittedFileDiff(arguments[0]));
                    break;
                case "list_commits_since_sha" when arguments.Length == 1:
                    WriteResult(await tools.ListCommitsSinceSha(arguments[0]));
                    break;
                case "list_commits_since_timestamp" when TryParseTimestamp(string.Join(" ", arguments), out var timestamp):
                    WriteResult(await tools.ListCommitsSinceTimestamp(timestamp));
                    break;
                case "get_current_changeset":
                    Console.WriteLine("Usage: get_current_changeset");
                    break;
                case "get_commit_by_sha":
                    Console.WriteLine("Usage: get_commit_by_sha <full-sha1>");
                    break;
                case "get_file_at_commit":
                    Console.WriteLine("Usage: get_file_at_commit <full-sha1> <repository-relative-path>");
                    break;
                case "get_uncommitted_file_diff":
                    Console.WriteLine("Usage: get_uncommitted_file_diff <repository-relative-path>");
                    break;
                case "list_commits_since_sha":
                    Console.WriteLine("Usage: list_commits_since_sha <full-sha1>");
                    break;
                case "list_commits_since_timestamp":
                    Console.WriteLine("Usage: list_commits_since_timestamp <timestamp>");
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}. Enter 'help' for available commands.");
                    break;
            }
        }
    }

    private static async Task<GitRepositoryContext> PromptForRepositoryAsync() {
        while (true) {
            Console.Write("Absolute repository path, '.' for the nearest parent repository, or 'exit' to quit: ");
            Console.Out.Flush();
            var repositoryPath = Console.ReadLine();
            if (repositoryPath is null || repositoryPath.Equals("exit", StringComparison.OrdinalIgnoreCase)) {
                Environment.Exit(0);
            }

            try {
                return await GitRepositoryContext.CreateAsync([ResolveRepositoryPath(repositoryPath)]);
            } catch (Exception exception) when (exception is ArgumentException or DirectoryNotFoundException or IOException) {
                Console.WriteLine(exception.Message);
            }
        }
    }

    private static string ResolveRepositoryPath(string repositoryPath) {
        if (repositoryPath != ".") {
            return repositoryPath;
        }

        for (DirectoryInfo? directory = new DirectoryInfo(Environment.CurrentDirectory); directory is not null; directory = directory.Parent) {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))) {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("No .git folder was found in the current directory or any parent directory.");
    }

    private static bool TryParseTimestamp(string input, out DateTimeOffset timestamp) {
        if (DateTimeOffset.TryParse(input, out timestamp)) {
            return true;
        }

        if (input.Length > 10 && input[10] == '-') {
            return DateTimeOffset.TryParse($"{input[..10]} {input[11..]}", out timestamp);
        }

        return false;
    }

    private static void WriteHelp() {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  get_current_changeset");
        Console.WriteLine("  get_commit_by_sha <full-sha1>");
        Console.WriteLine("  get_file_at_commit <full-sha1> <repository-relative-path>");
        Console.WriteLine("  get_uncommitted_file_diff <repository-relative-path>");
        Console.WriteLine("  list_commits_since_sha <full-sha1>");
        Console.WriteLine("  list_commits_since_timestamp <timestamp>");
        Console.WriteLine("  exit");
    }

    private static void WriteResult<T>(T result) {
        Console.WriteLine(JsonSerializer.Serialize(result, SerializerOptions));
    }
}

internal sealed record CommandLine(bool IsValid, IReadOnlyList<string> Arguments, string? Error) {
    public static CommandLine TryParse(string input) {
        var arguments = new List<string>();
        var currentArgument = new System.Text.StringBuilder();
        var isQuoted = false;
        var isEscaped = false;

        foreach (var character in input) {
            if (isEscaped) {
                currentArgument.Append(character);
                isEscaped = false;
                continue;
            }

            if (character == '\\') {
                isEscaped = true;
                continue;
            }

            if (character == '"') {
                isQuoted = !isQuoted;
                continue;
            }

            if (char.IsWhiteSpace(character) && !isQuoted) {
                AddCurrentArgument(arguments, currentArgument);
                continue;
            }

            currentArgument.Append(character);
        }

        if (isEscaped) {
            currentArgument.Append('\\');
        }

        if (isQuoted) {
            return new CommandLine(false, [], "Unterminated quoted argument.");
        }

        AddCurrentArgument(arguments, currentArgument);
        return new CommandLine(true, arguments, null);
    }

    private static void AddCurrentArgument(List<string> arguments, System.Text.StringBuilder currentArgument) {
        if (currentArgument.Length > 0) {
            arguments.Add(currentArgument.ToString());
            currentArgument.Clear();
        }
    }
}
