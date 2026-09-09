namespace HrManagement.Application.Authorization.Roles;

public sealed record AccountRoleAssignmentResult(
    bool IsSuccessful,
    Guid? AccountId = null,
    string? ErrorMessage = null);
