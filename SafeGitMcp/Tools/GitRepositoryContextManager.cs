namespace SafeGitMcp.Tools;

internal sealed class GitRepositoryContextManager : IDisposable {
    private readonly SemaphoreSlim _gate = new(1, 1);
    private GitRepositoryContext? _repository;

    public bool IsConfigured => Volatile.Read(ref _repository) is not null;

    public async Task<string> SetRepositoryRootAsync(string repositoryRoot) {
        var replacement = await GitRepositoryContext.CreateFromPathAsync(repositoryRoot);
        await _gate.WaitAsync();
        try {
            var previous = _repository;
            _repository = replacement;
            previous?.Dispose();
            return replacement.WorkingDirectoryPath;
        } finally {
            _gate.Release();
        }
    }

    public async Task<T> UseAsync<T>(Func<GitRepositoryContext, Task<T>> operation) {
        await _gate.WaitAsync();
        try {
            var repository = _repository ?? throw new InvalidOperationException("A repository root has not been configured.");
            return await operation(repository);
        } finally {
            _gate.Release();
        }
    }

    public void Dispose() {
        _gate.Wait();
        try {
            _repository?.Dispose();
            _repository = null;
        } finally {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
