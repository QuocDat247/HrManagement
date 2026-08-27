namespace HrManagement.Application.Workspaces.Overtime;

public sealed record OvertimeWorkspaceSnapshot(
    IReadOnlyList<OvertimeWorkspaceItem> Requests);
