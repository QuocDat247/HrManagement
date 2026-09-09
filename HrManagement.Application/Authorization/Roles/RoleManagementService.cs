using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public sealed class RoleManagementService
    : IRoleManagementService
{
    private readonly IRoleManagementPersistence
        _persistence;

    public RoleManagementService(
        IRoleManagementPersistence persistence)
    {
        _persistence =
            persistence;
    }

    public async Task<RoleManagementResult> CreateAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        Role role;

        try
        {
            role =
                new Role(
                    Guid.NewGuid(),
                    name,
                    description);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        RoleManagementPersistenceResult result =
            await _persistence
                .TryCreateAsync(
                    role,
                    cancellationToken);

        return result switch
        {
            RoleManagementPersistenceResult.Created =>
                new RoleManagementResult(
                    true,
                    role.Id),

            RoleManagementPersistenceResult.NameAlreadyExists =>
                Failure(
                    "Tên vai trò đã tồn tại."),

            _ =>
                Failure(
                    "Không thể tạo vai trò.")
        };
    }

    public async Task<RoleManagementResult> UpdateAsync(
        Guid roleId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (roleId == Guid.Empty)
        {
            return Failure(
                "Vai trò không hợp lệ.");
        }

        Role candidate;

        try
        {
            candidate =
                new Role(
                    roleId,
                    name,
                    description);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        RoleManagementPersistenceResult result =
            await _persistence
                .TryUpdateAsync(
                    roleId,
                    candidate.Name,
                    candidate.Description,
                    cancellationToken);

        return result switch
        {
            RoleManagementPersistenceResult.Updated =>
                new RoleManagementResult(
                    true,
                    roleId),

            RoleManagementPersistenceResult.RoleNotFound =>
                Failure(
                    "Không tìm thấy vai trò."),

            RoleManagementPersistenceResult.NameAlreadyExists =>
                Failure(
                    "Tên vai trò đã tồn tại."),

            _ =>
                Failure(
                    "Không thể cập nhật vai trò.")
        };
    }

    private static RoleManagementResult Failure(
        string message)
    {
        return new RoleManagementResult(
            false,
            ErrorMessage:
                message);
    }
}
