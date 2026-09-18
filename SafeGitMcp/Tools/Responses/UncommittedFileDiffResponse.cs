using SafeGitMcp.Tools;

namespace SafeGitMcp.Tools.Responses;

public sealed class UncommittedFileDiffResponse {
    private readonly CurrentChangesetFile? _file;
    private readonly string? _error;

    private UncommittedFileDiffResponse(CurrentChangesetFile file) {
        _file = file;
    }

    private UncommittedFileDiffResponse(string error) {
        _error = error;
    }

    public bool Found => _file is not null;
    public string? Error => _error;
    public CurrentChangesetFile? File => _file;

    public static UncommittedFileDiffResponse Failure(string error) {
        return new UncommittedFileDiffResponse(error);
    }

    public static UncommittedFileDiffResponse FromFile(CurrentChangesetFile file) {
        return new UncommittedFileDiffResponse(file);
    }
}
