using GitReader.Structures;

namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryContext : IDisposable {
    private GitRepositoryContext(StructuredRepository repository, string workingDirectoryPath) {
        Repository = repository;
        WorkingDirectoryPath = workingDirectoryPath;
    }

    public StructuredRepository Repository { get; }
    public string WorkingDirectoryPath { get; }

    public static async Task<GitRepositoryContext> CreateAsync(string[] args, CancellationToken cancellationToken = default) {
        var repositoryPath = GetRepositoryPathArgument(args);

        if (!Path.IsPathFullyQualified(repositoryPath)) {
            throw new ArgumentException("The repository path must be absolute.");
        }

        var fullPath = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(fullPath)) {
            throw new DirectoryNotFoundException($"The repository path does not exist: {fullPath}");
        }

        var repository = await GitReader.Repository.Factory.OpenStructureAsync(fullPath, cancellationToken);
        return new GitRepositoryContext(repository, fullPath);
    }

    public void Dispose() {
        Repository.Dispose();
    }

    private static string GetRepositoryPathArgument(string[] args) {
        if (args.Length == 1 && !args[0].StartsWith("--", StringComparison.Ordinal)) {
            return args[0];
        }

        for (var index = 0; index < args.Length; index++) {
            if (args[index] == "--repository" && index + 1 < args.Length) {
                return args[index + 1];
            }
        }

        throw new ArgumentException("Pass the target repository as an absolute path or with --repository <absolute-path>.");
    }
}
