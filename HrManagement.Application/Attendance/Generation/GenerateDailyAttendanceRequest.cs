namespace HrManagement.Application.Attendance.Generation;

public sealed record GenerateDailyAttendanceRequest(
    DateOnly WorkDate,
    Guid? EmployeeId = null);
