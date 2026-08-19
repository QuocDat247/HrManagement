using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class EfEmployeeWorkScheduleAssignmentPersistence
    : IEmployeeWorkScheduleAssignmentPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeWorkScheduleAssignmentPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task ApplyAsync(
        EmployeeWorkScheduleAssignment? closedAssignment,
        EmployeeWorkScheduleAssignment newAssignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            newAssignment);

        ValidateRequestedTransition(
            closedAssignment,
            newAssignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        if (closedAssignment is not null)
        {
            EmployeeWorkScheduleAssignment? persisted =
                await dbContext
                    .EmployeeWorkScheduleAssignments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        assignment =>
                            assignment.Id ==
                            closedAssignment.Id,
                        cancellationToken);

            ValidatePersistedAssignment(
                persisted,
                closedAssignment);

            int updatedRows =
                await dbContext
                    .EmployeeWorkScheduleAssignments
                    .Where(
                        assignment =>
                            assignment.Id ==
                                closedAssignment.Id
                            && assignment.EmployeeId ==
                                closedAssignment.EmployeeId
                            && assignment.EmploymentPeriodId ==
                                closedAssignment.EmploymentPeriodId
                            && assignment.EffectiveTo ==
                                null)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                assignment =>
                                    assignment.EffectiveTo,
                                closedAssignment.EffectiveTo),
                        cancellationToken);

            if (updatedRows != 1)
            {
                throw new DbUpdateConcurrencyException(
                    "Phân lịch làm việc hiện tại đã thay đổi trong quá trình cập nhật.");
            }
        }

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
                newAssignment,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateRequestedTransition(
        EmployeeWorkScheduleAssignment? closedAssignment,
        EmployeeWorkScheduleAssignment newAssignment)
    {
        if (!newAssignment.IsOpen)
        {
            throw new ArgumentException(
                "Phân lịch làm việc mới phải đang mở.",
                nameof(newAssignment));
        }

        if (closedAssignment is null)
        {
            return;
        }

        if (closedAssignment.IsOpen)
        {
            throw new ArgumentException(
                "Phân lịch làm việc cũ phải được kết thúc.",
                nameof(closedAssignment));
        }

        if (closedAssignment.Id ==
            newAssignment.Id)
        {
            throw new ArgumentException(
                "Phân lịch mới phải có mã định danh khác.",
                nameof(newAssignment));
        }

        if (closedAssignment.EmployeeId !=
            newAssignment.EmployeeId)
        {
            throw new ArgumentException(
                "Hai phân lịch phải thuộc cùng nhân viên.");
        }

        if (closedAssignment.EmploymentPeriodId !=
            newAssignment.EmploymentPeriodId)
        {
            throw new ArgumentException(
                "Hai phân lịch phải thuộc cùng giai đoạn làm việc.");
        }

        DateOnly expectedEffectiveFrom =
            closedAssignment.EffectiveTo!.Value
                .AddDays(
                    1);

        if (newAssignment.EffectiveFrom !=
            expectedEffectiveFrom)
        {
            throw new ArgumentException(
                "Ngày bắt đầu lịch mới phải ngay sau ngày kết thúc lịch cũ.");
        }
    }

    private static void ValidatePersistedAssignment(
        EmployeeWorkScheduleAssignment? persisted,
        EmployeeWorkScheduleAssignment requestedClosedAssignment)
    {
        if (persisted is null)
        {
            throw new DbUpdateConcurrencyException(
                "Không còn tìm thấy phân lịch làm việc hiện tại.");
        }

        if (!persisted.IsOpen)
        {
            throw new DbUpdateConcurrencyException(
                "Phân lịch làm việc hiện tại đã được kết thúc.");
        }

        if (persisted.EmployeeId !=
                requestedClosedAssignment.EmployeeId
            || persisted.EmploymentPeriodId !=
                requestedClosedAssignment.EmploymentPeriodId
            || persisted.WorkScheduleId !=
                requestedClosedAssignment.WorkScheduleId
            || persisted.EffectiveFrom !=
                requestedClosedAssignment.EffectiveFrom)
        {
            throw new DbUpdateConcurrencyException(
                "Phân lịch làm việc hiện tại không còn khớp dữ liệu đã đọc.");
        }
    }
}
