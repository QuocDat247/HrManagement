using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Leave.Types;

public sealed class LeaveTypeSeedService
{
    public static readonly Guid AnnualLeaveTypeId =
        Guid.Parse(
            "17040000-0000-0000-0000-000000000001");

    public static readonly Guid SickLeaveTypeId =
        Guid.Parse(
            "17040000-0000-0000-0000-000000000002");

    public static readonly Guid UnpaidLeaveTypeId =
        Guid.Parse(
            "17040000-0000-0000-0000-000000000003");

    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public LeaveTypeSeedService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        List<LeaveType> existingTypes =
            await dbContext
                .LeaveTypes
                .AsNoTracking()
                .ToListAsync(
                    cancellationToken);

        LeaveType[] defaults =
        [
            new LeaveType(
                AnnualLeaveTypeId,
                "ANNUAL",
                "Nghỉ phép năm",
                isPaid: true),

            new LeaveType(
                SickLeaveTypeId,
                "SICK",
                "Nghỉ ốm",
                isPaid: true),

            new LeaveType(
                UnpaidLeaveTypeId,
                "UNPAID",
                "Nghỉ không lương",
                isPaid: false)
        ];

        foreach (LeaveType defaultType in defaults)
        {
            LeaveType? existingByCode =
                existingTypes
                    .SingleOrDefault(
                        existing =>
                            existing.Code ==
                            defaultType.Code);

            if (existingByCode is not null)
            {
                continue;
            }

            LeaveType? existingById =
                existingTypes
                    .SingleOrDefault(
                        existing =>
                            existing.Id ==
                            defaultType.Id);

            if (existingById is not null)
            {
                throw new InvalidOperationException(
                    $"Mã định danh mặc định của loại nghỉ phép {defaultType.Code} đã được sử dụng.");
            }

            await dbContext
                .LeaveTypes
                .AddAsync(
                    defaultType,
                    cancellationToken);
        }

        if (!dbContext.ChangeTracker.HasChanges())
        {
            return;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
