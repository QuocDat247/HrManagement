namespace HrManagement.Application.Organization.Positions;

public sealed record UpdatePositionRequest(
    Guid PositionId,
    string Code,
    string Name);
