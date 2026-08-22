using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public sealed class WorkScheduleDayManagementService
    : IWorkScheduleDayManagementService
{
    private readonly IWorkScheduleManagementPersistence
        _schedulePersistence;

    private readonly IWorkScheduleDayManagementPersistence
        _dayPersistence;

    public WorkScheduleDayManagementService(
        IWorkScheduleManagementPersistence schedulePersistence,
        IWorkScheduleDayManagementPersistence dayPersistence)
    {
        _schedulePersistence =
            schedulePersistence;

        _dayPersistence =
            dayPersistence;
    }

    public async Task<WorkScheduleDayManagementResult> UpdateAsync(
        UpdateWorkScheduleDayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.WorkScheduleId ==
            Guid.Empty)
        {
            return Failure(
                "Mã lịch làm việc không hợp lệ.");
        }

        if (!Enum.IsDefined(
                request.DayOfWeek))
        {
            return Failure(
                "Ngày trong tuần không hợp lệ.");
        }

        WorkSchedule? schedule =
            await _schedulePersistence
                .GetByIdAsync(
                    request.WorkScheduleId,
                    cancellationToken);

        if (schedule is null)
        {
            return Failure(
                "Không tìm thấy lịch làm việc.");
        }

        WorkScheduleDay? existing =
            await _dayPersistence
                .GetAsync(
                    schedule.Id,
                    request.DayOfWeek,
                    cancellationToken);

        if (existing is null)
        {
            return Failure(
                "Không tìm thấy cấu hình ngày làm việc.");
        }

        WorkScheduleDay updated;

        try
        {
            updated =
                new WorkScheduleDay(
                    existing.Id,
                    existing.WorkScheduleId,
                    existing.DayOfWeek,
                    request.IsWorkingDay,
                    request.StartTime,
                    request.EndTime,
                    request.BreakMinutes);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        await _dayPersistence.UpdateAsync(
            updated,
            cancellationToken);

        return new WorkScheduleDayManagementResult(
            true);
    }

    private static WorkScheduleDayManagementResult Failure(
        string errorMessage)
    {
        return new WorkScheduleDayManagementResult(
            false,
            errorMessage);
    }
}
