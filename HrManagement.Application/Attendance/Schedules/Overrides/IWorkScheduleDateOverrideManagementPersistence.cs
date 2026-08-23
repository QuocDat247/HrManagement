using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules.Overrides;

public interface IWorkScheduleDateOverrideManagementPersistence
{
    Task<WorkScheduleDateOverride?> GetByIdAsync(
        Guid workScheduleDateOverrideId,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleDateOverride?> GetByScheduleAndDateAsync(
        Guid workScheduleId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        WorkScheduleDateOverride item,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WorkScheduleDateOverride item,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid workScheduleDateOverrideId,
        CancellationToken cancellationToken = default);
}
