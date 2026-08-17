using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees.Profiles;

public sealed class EfEmployeeIdentificationRecordRepository
    : IEmployeeIdentificationRecordRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeIdentificationRecordRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<EmployeeIdentificationRecord>>
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
            .EmployeeIdentificationRecords
            .AsNoTracking()
            .Where(
                record =>
                    record.EmployeeId ==
                    employeeId)
            .OrderBy(
                record =>
                    record.Type)
            .ThenByDescending(
                record =>
                    record.IssueDate)
            .ThenBy(
                record =>
                    record.Id)
            .ToListAsync(
                cancellationToken);
    }

    public async Task UpsertAsync(
        EmployeeIdentificationRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        EmployeeIdentificationRecord? existing =
            await dbContext
                .EmployeeIdentificationRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        record.Id,
                    cancellationToken);

        if (existing is not null
            && existing.EmployeeId !=
                record.EmployeeId)
        {
            throw new InvalidOperationException(
                "Không thể chuyển giấy tờ định danh sang nhân viên khác.");
        }

        if (existing is null)
        {
            await dbContext
                .EmployeeIdentificationRecords
                .AddAsync(
                    record,
                    cancellationToken);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return;
        }

        await dbContext
            .EmployeeIdentificationRecords
            .Where(
                item =>
                    item.Id ==
                    record.Id)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(
                            item =>
                                item.Type,
                            record.Type)
                        .SetProperty(
                            item =>
                                item.DocumentNumber,
                            record.DocumentNumber)
                        .SetProperty(
                            item =>
                                item.IssueDate,
                            record.IssueDate)
                        .SetProperty(
                            item =>
                                item.ExpiryDate,
                            record.ExpiryDate)
                        .SetProperty(
                            item =>
                                item.IssuingAuthority,
                            record.IssuingAuthority)
                        .SetProperty(
                            item =>
                                item.PlaceOfIssue,
                            record.PlaceOfIssue)
                        .SetProperty(
                            item =>
                                item.IssuingCountry,
                            record.IssuingCountry),
                cancellationToken);
    }

    public async Task DeleteAsync(
        Guid employeeId,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (recordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giấy tờ không hợp lệ.",
                nameof(recordId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await dbContext
            .EmployeeIdentificationRecords
            .Where(
                record =>
                    record.EmployeeId ==
                        employeeId
                    && record.Id ==
                        recordId)
            .ExecuteDeleteAsync(
                cancellationToken);
    }
}
