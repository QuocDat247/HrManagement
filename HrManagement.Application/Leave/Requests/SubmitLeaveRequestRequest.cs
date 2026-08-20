namespace HrManagement.Application.Leave.Requests;

public sealed record SubmitLeaveRequestRequest(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason);
