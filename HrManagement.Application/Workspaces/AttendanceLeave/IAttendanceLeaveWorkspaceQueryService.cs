namespace HrManagement.Application.Workspaces.AttendanceLeave;

public interface IAttendanceLeaveWorkspaceQueryService
{
    Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
        GetEmployeesAsync(
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
        GetActiveLeaveTypesAsync(
            CancellationToken cancellationToken = default);

    Task<AttendanceLeaveWorkspaceSnapshot> GetAsync(
        AttendanceLeaveWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
