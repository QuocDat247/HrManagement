namespace HrManagement.Application.Attendance.Schedules;

public interface IWorkScheduleDayManagementService
{
    Task<WorkScheduleDayManagementResult> UpdateAsync(
        UpdateWorkScheduleDayRequest request,
        CancellationToken cancellationToken = default);
}
