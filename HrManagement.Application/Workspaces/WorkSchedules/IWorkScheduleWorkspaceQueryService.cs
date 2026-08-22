namespace HrManagement.Application.Workspaces.WorkSchedules;

public interface IWorkScheduleWorkspaceQueryService
{
    Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
        GetEmployeesAsync(
            CancellationToken cancellationToken = default);

    Task<WorkScheduleWorkspaceSnapshot> GetAsync(
        WorkScheduleWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
