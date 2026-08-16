using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees.Profiles;

public sealed class EfEmployeeEmergencyContactRepository
    : IEmployeeEmergencyContactRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeEmergencyContactRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
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

        if (contact.IsPrimary)
        {
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
        }

        if (existing is null)
        {
            await dbContext
                .EmployeeEmergencyContacts
                .AddAsync(
                    contact,
                    cancellationToken);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        else
        {
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
        }

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
    }
}
