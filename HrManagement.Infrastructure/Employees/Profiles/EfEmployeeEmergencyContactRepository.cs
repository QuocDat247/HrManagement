using HrManagement.Application.Auditing;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees.Profiles;

public sealed class EfEmployeeEmergencyContactRepository
    : IEmployeeEmergencyContactRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IAuditEntryFactory
        _auditEntryFactory;

    public EfEmployeeEmergencyContactRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task<IReadOnlyList<EmployeeEmergencyContact>>
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
            .EmployeeEmergencyContacts
            .AsNoTracking()
            .Where(
                contact =>
                    contact.EmployeeId ==
                    employeeId)
            .OrderByDescending(
                contact =>
                    contact.IsPrimary)
            .ThenBy(
                contact =>
                    contact.FullName)
            .ThenBy(
                contact =>
                    contact.Id)
            .ToListAsync(
                cancellationToken);
    }

    public async Task UpsertAsync(
        EmployeeEmergencyContact contact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            contact);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        EmployeeEmergencyContact? existing =
            await dbContext
                .EmployeeEmergencyContacts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        contact.Id,
                    cancellationToken);

        if (existing is not null
            && existing.EmployeeId !=
                contact.EmployeeId)
        {
            throw new InvalidOperationException(
                "Không thể chuyển liên hệ khẩn cấp sang nhân viên khác.");
        }

        Guid? previousPrimaryId =
            null;

        if (contact.IsPrimary)
        {
            previousPrimaryId =
                await dbContext
                    .EmployeeEmergencyContacts
                    .Where(
                        item =>
                            item.EmployeeId ==
                                contact.EmployeeId
                            && item.Id !=
                                contact.Id
                            && item.IsPrimary)
                    .Select(
                        item =>
                            (Guid?)item.Id)
                    .SingleOrDefaultAsync(
                        cancellationToken);
        }

        AuditAction contactAction =
            existing is null
                ? AuditAction.Created
                : AuditAction.Updated;

        AuditEntry contactAudit =
            _auditEntryFactory.Create(
                contactAction,
                AuditEntityTypes.EmployeeEmergencyContact,
                contact.Id,
                contact.EmployeeId);

        AuditEntry? previousPrimaryAudit =
            previousPrimaryId.HasValue
                ? _auditEntryFactory.Create(
                    AuditAction.Updated,
                    AuditEntityTypes.EmployeeEmergencyContact,
                    previousPrimaryId.Value,
                    contact.EmployeeId)
                : null;

        if (contact.IsPrimary)
        {
            int demotedRows =
                await dbContext
                    .EmployeeEmergencyContacts
                    .Where(
                        item =>
                            item.EmployeeId ==
                                contact.EmployeeId
                            && item.Id !=
                                contact.Id
                            && item.IsPrimary)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                item =>
                                    item.IsPrimary,
                                false),
                        cancellationToken);

            int expectedDemotedRows =
                previousPrimaryId.HasValue
                    ? 1
                    : 0;

            if (demotedRows !=
                expectedDemotedRows)
            {
                throw new DbUpdateConcurrencyException(
                    "Liên hệ chính đã thay đổi trong quá trình cập nhật.");
            }
        }

        if (existing is null)
        {
            await dbContext
                .EmployeeEmergencyContacts
                .AddAsync(
                    contact,
                    cancellationToken);
        }
        else
        {
            int updatedRows =
                await dbContext
                    .EmployeeEmergencyContacts
                    .Where(
                        item =>
                            item.Id ==
                            contact.Id)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(
                                    item =>
                                        item.FullName,
                                    contact.FullName)
                                .SetProperty(
                                    item =>
                                        item.Relationship,
                                    contact.Relationship)
                                .SetProperty(
                                    item =>
                                        item.PhoneNumber,
                                    contact.PhoneNumber)
                                .SetProperty(
                                    item =>
                                        item.Email,
                                    contact.Email)
                                .SetProperty(
                                    item =>
                                        item.IsPrimary,
                                    contact.IsPrimary),
                        cancellationToken);

            if (updatedRows != 1)
            {
                throw new DbUpdateConcurrencyException(
                    "Liên hệ khẩn cấp đã thay đổi trong quá trình cập nhật.");
            }
        }

        await dbContext.AuditEntries
            .AddAsync(
                contactAudit,
                cancellationToken);

        if (previousPrimaryAudit is not null)
        {
            await dbContext.AuditEntries
                .AddAsync(
                    previousPrimaryAudit,
                    cancellationToken);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid employeeId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (contactId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã liên hệ khẩn cấp không hợp lệ.",
                nameof(contactId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        bool exists =
            await dbContext
                .EmployeeEmergencyContacts
                .AnyAsync(
                    contact =>
                        contact.Id ==
                            contactId
                        && contact.EmployeeId ==
                            employeeId,
                    cancellationToken);

        if (!exists)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return;
        }

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                AuditAction.Deleted,
                AuditEntityTypes.EmployeeEmergencyContact,
                contactId,
                employeeId);

        int deletedRows =
            await dbContext
                .EmployeeEmergencyContacts
                .Where(
                    contact =>
                        contact.Id ==
                            contactId
                        && contact.EmployeeId ==
                            employeeId)
                .ExecuteDeleteAsync(
                    cancellationToken);

        if (deletedRows != 1)
        {
            throw new DbUpdateConcurrencyException(
                "Liên hệ khẩn cấp đã thay đổi trong quá trình xóa.");
        }

        await dbContext.AuditEntries
            .AddAsync(
                auditEntry,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }
}
