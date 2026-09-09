namespace HrManagement.Application.Authorization.Roles;

public sealed record RoleManagementResult(
    bool IsSuccessful,
    Guid? RoleId = null,
    string? ErrorMessage = null);
