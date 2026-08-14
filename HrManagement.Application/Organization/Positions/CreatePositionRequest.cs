namespace HrManagement.Application.Organization.Positions;

public sealed record CreatePositionRequest(
    string Code,
    string Name);
