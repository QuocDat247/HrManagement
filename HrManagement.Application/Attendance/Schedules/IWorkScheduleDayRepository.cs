using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public interface IWorkScheduleDayRepository
{
    Task<IReadOnlyList<WorkScheduleDay>>
        GetByWorkScheduleIdAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default);
}
