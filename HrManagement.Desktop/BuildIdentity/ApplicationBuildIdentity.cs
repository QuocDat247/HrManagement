namespace HrManagement.Desktop.BuildIdentity;

public sealed record ApplicationBuildIdentity(
    string CoreVersion,
    string Edition,
    string CustomerCode,
    string ReleaseChannel);
