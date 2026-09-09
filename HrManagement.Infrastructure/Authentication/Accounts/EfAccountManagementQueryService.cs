using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Accounts;

public sealed class EfAccountManagementQueryService
    : IAccountManagementQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAccountManagementQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<AccountManagementSnapshot> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        List<UserAccount> accounts =
            await dbContext
                .UserAccounts
                .AsNoTracking()
                .OrderBy(
                    account =>
                        account.Username)
                .ThenBy(
                    account =>
                        account.Id)
                .ToListAsync(
                    cancellationToken);

        List<AccountManagementRoleItem> roles =
            await dbContext
                .Roles
                .AsNoTracking()
                .OrderBy(
                    role =>
                        role.Name)
                .ThenBy(
                    role =>
                        role.Id)
                .Select(
                    role =>
                        new AccountManagementRoleItem(
                            role.Id,
                            role.Name,
                            role.Description,
                            role.IsActive))
                .ToListAsync(
                    cancellationToken);

        List<UserAccountRole> assignments =
            await dbContext
                .UserAccountRoles
                .AsNoTracking()
                .ToListAsync(
                    cancellationToken);

        List<AccountManagementEmployeeItem> employees =
            await dbContext
                .Employees
                .AsNoTracking()
                .OrderBy(
                    employee =>
                        employee.EmployeeCode)
                .ThenBy(
                    employee =>
                        employee.FullName)
                .ThenBy(
                    employee =>
                        employee.Id)
                .Select(
                    employee =>
                        new AccountManagementEmployeeItem(
                            employee.Id,
                            employee.EmployeeCode,
                            employee.FullName,
                            employee.Status))
                .ToListAsync(
                    cancellationToken);

        Dictionary<Guid, AccountManagementRoleItem>
            rolesById =
                roles.ToDictionary(
                    role =>
                        role.RoleId);

        ILookup<Guid, UserAccountRole>
            assignmentsByAccountId =
                assignments.ToLookup(
                    assignment =>
                        assignment.AccountId);

        Dictionary<Guid, AccountManagementEmployeeItem>
            employeesById =
                employees.ToDictionary(
                    employee =>
                        employee.EmployeeId);

        var accountItems =
            new List<AccountManagementAccountItem>(
                accounts.Count);

        foreach (UserAccount account in accounts)
        {
            AccountManagementRoleItem[] assignedRoles =
                assignmentsByAccountId[
                        account.Id]
                    .Select(
                        assignment =>
                            rolesById.TryGetValue(
                                assignment.RoleId,
                                out AccountManagementRoleItem?
                                    role)
                                ? role
                                : throw new InvalidOperationException(
                                    "Dữ liệu phân quyền của tài khoản không nhất quán."))
                    .OrderBy(
                        role =>
                            role.Name)
                    .ThenBy(
                        role =>
                            role.RoleId)
                    .ToArray();

            AccountManagementEmployeeItem?
                linkedEmployee =
                    null;

            if (account.EmployeeId.HasValue)
            {
                if (!employeesById.TryGetValue(
                        account.EmployeeId.Value,
                        out linkedEmployee))
                {
                    throw new InvalidOperationException(
                        "Dữ liệu liên kết nhân viên của tài khoản không nhất quán.");
                }
            }

            accountItems.Add(
                new AccountManagementAccountItem(
                    account.Id,
                    account.Username,
                    account.DisplayName,
                    account.Kind,
                    account.IsActive,
                    account.EmployeeId,
                    linkedEmployee?.EmployeeCode,
                    linkedEmployee?.FullName,
                    assignedRoles));
        }

        return new AccountManagementSnapshot(
            accountItems,
            roles,
            employees);
    }
}
