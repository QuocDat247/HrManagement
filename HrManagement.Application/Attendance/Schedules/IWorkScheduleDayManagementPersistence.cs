using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public interface IWorkScheduleDayManagementPersistence
{
    Task<WorkScheduleDay?> GetAsync(
        Guid workScheduleId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WorkScheduleDay day,
        CancellationToken cancellationToken = default);
}
