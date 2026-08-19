using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Application.Auditing;
using HrManagement.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees.Profiles;

public sealed class EfEmployeeAddressRepository
    : IEmployeeAddressRepository
{
    private readonly IAuditEntryFactory
        _auditEntryFactory;

    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    // Constructor
    public EfEmployeeAddressRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task<IReadOnlyList<EmployeeAddress>>
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
            .EmployeeAddresses
            .AsNoTracking()
            .Where(
                address =>
                    address.EmployeeId ==
                    employeeId)
            .OrderBy(
                address =>
                    address.Type)
            .ToListAsync(
                cancellationToken);
    }

    public async Task UpsertAsync(
    EmployeeAddress address,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            address);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        Guid? existingId =
            await dbContext
                .EmployeeAddresses
                .Where(
                    existing =>
                        existing.EmployeeId ==
                            address.EmployeeId
                        && existing.Type ==
                            address.Type)
                .Select(
                    existing =>
                        (Guid?)existing.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        AuditAction action =
            existingId.HasValue
                ? AuditAction.Updated
                : AuditAction.Created;

        Guid auditedEntityId =
            existingId
            ?? address.Id;

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                action,
                AuditEntityTypes.EmployeeAddress,
                auditedEntityId,
                address.EmployeeId);

        if (!existingId.HasValue)
        {
            await dbContext
                .EmployeeAddresses
                .AddAsync(
                    address,
                    cancellationToken);
        }
        else
        {
            int updatedRows =
                await dbContext
                    .EmployeeAddresses
                    .Where(
                        existing =>
                            existing.Id ==
                            existingId.Value)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(
                                    existing =>
                                        existing.AddressLine,
                                    address.AddressLine)
                                .SetProperty(
                                    existing =>
                                        existing.Ward,
                                    address.Ward)
                                .SetProperty(
                                    existing =>
                                        existing.District,
                                    address.District)
                                .SetProperty(
                                    existing =>
                                        existing.Province,
                                    address.Province)
                                .SetProperty(
                                    existing =>
                                        existing.Country,
                                    address.Country)
                                .SetProperty(
                                    existing =>
                                        existing.PostalCode,
                                    address.PostalCode),
                        cancellationToken);

            if (updatedRows != 1)
            {
                throw new DbUpdateConcurrencyException(
                    "Địa chỉ đã thay đổi trong quá trình cập nhật.");
            }
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

    public async Task DeleteAsync(
    Guid employeeId,
    EmployeeAddressType type,
    CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        Guid? existingId =
            await dbContext
                .EmployeeAddresses
                .Where(
                    address =>
                        address.EmployeeId ==
                            employeeId
                        && address.Type ==
                            type)
                .Select(
                    address =>
                        (Guid?)address.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!existingId.HasValue)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return;
        }

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                AuditAction.Deleted,
                AuditEntityTypes.EmployeeAddress,
                existingId.Value,
                employeeId);

        int deletedRows =
            await dbContext
                .EmployeeAddresses
                .Where(
                    address =>
                        address.Id ==
                            existingId.Value
                        && address.EmployeeId ==
                            employeeId)
                .ExecuteDeleteAsync(
                    cancellationToken);

        if (deletedRows != 1)
        {
            throw new DbUpdateConcurrencyException(
                "Địa chỉ đã thay đổi trong quá trình xóa.");
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
