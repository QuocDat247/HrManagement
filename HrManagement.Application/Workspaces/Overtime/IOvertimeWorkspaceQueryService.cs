namespace HrManagement.Application.Workspaces.Overtime;

public interface IOvertimeWorkspaceQueryService
{
    Task<OvertimeWorkspaceSnapshot> GetAsync(
        OvertimeWorkspaceQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OvertimeEmployeeOption>> GetEmployeesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OvertimeStatusHistoryItem>> GetHistoryAsync(
        Guid overtimeRequestId,
        CancellationToken cancellationToken = default);
}
