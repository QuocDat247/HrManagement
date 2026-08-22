namespace HrManagement.Application.Attendance.Schedules;

public interface IWorkScheduleManagementService
{
    Task<WorkScheduleManagementResult> CreateAsync(
        CreateWorkScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleManagementResult> CloneAsync(
        CloneWorkScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleManagementResult> UpdateAsync(
        UpdateWorkScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleManagementResult> DeactivateAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleManagementResult> ReactivateAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleManagementResult> DeleteAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default);
}
