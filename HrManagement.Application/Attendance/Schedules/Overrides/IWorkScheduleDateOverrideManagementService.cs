namespace HrManagement.Application.Attendance.Schedules.Overrides;

public interface IWorkScheduleDateOverrideManagementService
{
    Task<WorkScheduleDateOverrideManagementResult> CreateAsync(
        CreateWorkScheduleDateOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleDateOverrideManagementResult> UpdateAsync(
        UpdateWorkScheduleDateOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkScheduleDateOverrideManagementResult> DeleteAsync(
        Guid workScheduleDateOverrideId,
        CancellationToken cancellationToken = default);
}
