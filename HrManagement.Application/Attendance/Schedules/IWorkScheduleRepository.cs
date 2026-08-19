using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public interface IWorkScheduleRepository
{
    Task<WorkSchedule?> GetByIdAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);
}
