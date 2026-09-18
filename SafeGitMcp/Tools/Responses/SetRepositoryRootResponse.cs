namespace SafeGitMcp.Tools.Responses;

public sealed class SetRepositoryRootResponse {
    private readonly string? _repositoryRoot;
    private readonly string? _error;

    private SetRepositoryRootResponse(string repositoryRoot, bool success) {
        _repositoryRoot = repositoryRoot;
        Success = success;
    }

    private SetRepositoryRootResponse(Exception exception) {
        _error = exception.Message;
    }

    public bool Success { get; }
    public string? RepositoryRoot => _repositoryRoot;
    public string? Error => _error;

    public static SetRepositoryRootResponse Configured(string repositoryRoot) {
        return new SetRepositoryRootResponse(repositoryRoot, true);
    }

    public static SetRepositoryRootResponse Failure(Exception exception) {
        return new SetRepositoryRootResponse(exception);
    }
}
