using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Workspaces.Overtime;

public sealed record OvertimeWorkspaceQuery(
    int Year,
    int Month,
    Guid? EmployeeId = null,
    OvertimeRequestStatus? Status = null);
