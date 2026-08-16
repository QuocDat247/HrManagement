using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees.Profiles;

public sealed class EfEmployeeAddressRepository
    : IEmployeeAddressRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeAddressRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
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

        if (!existingId.HasValue)
        {
            await dbContext
                .EmployeeAddresses
                .AddAsync(
                    address,
                    cancellationToken);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            return;
        }

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

        await dbContext
            .EmployeeAddresses
            .Where(
                address =>
                    address.EmployeeId ==
                        employeeId
                    && address.Type ==
                        type)
            .ExecuteDeleteAsync(
                cancellationToken);
    }
}
