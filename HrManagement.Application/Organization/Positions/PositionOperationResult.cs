namespace HrManagement.Application.Organization.Positions;

public sealed record PositionOperationResult(
    bool IsSuccessful,
    string? ErrorMessage);
