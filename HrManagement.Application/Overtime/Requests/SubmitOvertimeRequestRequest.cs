namespace HrManagement.Application.Overtime.Requests;

public sealed record SubmitOvertimeRequestRequest(
    Guid EmployeeId,
    DateOnly WorkDate,
    int RequestedMinutes,
    string? Reason);
