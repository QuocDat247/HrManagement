namespace HrManagement.Application.Authorization.Roles;

public sealed record ReplaceAccountRolesRequest(
    Guid AccountId,
    IReadOnlyCollection<Guid> RoleIds);
