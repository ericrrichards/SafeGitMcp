namespace SafeGitMcp.Tools.Responses;

public sealed class CommitFileLookupResponse {
    private readonly CommitFileDetailsResponse? _file;
    private readonly string? _error;

    private CommitFileLookupResponse(CommitFileDetailsResponse file) {
        _file = file;
    }

    private CommitFileLookupResponse(string error) {
        _error = error;
    }

    public bool Found => _file is not null;
    public string? Error => _error;
    public CommitFileDetailsResponse? File => _file;

    public static CommitFileLookupResponse Failure(string error) {
        return new CommitFileLookupResponse(error);
    }

    public static CommitFileLookupResponse FromFile(CommitFileDetailsResponse file) {
        return new CommitFileLookupResponse(file);
    }
}
