namespace HrManagement.Application.Attendance.Calculations;

public sealed record ApprovedLeaveAttendanceInput(
    Guid LeaveRequestId,
    Guid LeaveTypeId);
