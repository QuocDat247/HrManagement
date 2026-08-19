using HrManagement.Application.Auditing;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees.Profiles;

public sealed class EfEmployeePersonalProfileRepository
    : IEmployeePersonalProfileRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IAuditEntryFactory
        _auditEntryFactory;

    public EfEmployeePersonalProfileRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task<EmployeePersonalProfile?>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .EmployeePersonalProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                profile =>
                    profile.EmployeeId ==
                    employeeId,
                cancellationToken);
    }

    public async Task UpsertAsync(
        EmployeePersonalProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        bool exists =
            await dbContext
                .EmployeePersonalProfiles
                .AnyAsync(
                    existing =>
                        existing.EmployeeId ==
                        profile.EmployeeId,
                    cancellationToken);

        AuditAction action;

        if (exists)
        {
            dbContext
                .EmployeePersonalProfiles
                .Update(
                    profile);

            action =
                AuditAction.Updated;
        }
        else
        {
            await dbContext
                .EmployeePersonalProfiles
                .AddAsync(
                    profile,
                    cancellationToken);

            action =
                AuditAction.Created;
        }

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                action,
                AuditEntityTypes.EmployeePersonalProfile,
                profile.EmployeeId,
                profile.EmployeeId);

        await dbContext
            .AuditEntries
            .AddAsync(
                auditEntry,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
