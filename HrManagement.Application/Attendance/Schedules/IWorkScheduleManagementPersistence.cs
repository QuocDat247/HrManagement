using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public interface IWorkScheduleManagementPersistence
{
    Task<WorkSchedule?> GetByIdAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);

    Task<WorkSchedule?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        WorkSchedule schedule,
        IReadOnlyList<WorkScheduleDay> days,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WorkSchedule schedule,
        CancellationToken cancellationToken = default);

    Task<bool> IsInUseAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);
}
