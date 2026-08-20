using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Records;

public sealed record RecordAttendancePunchRequest(
    Guid EmployeeId,
    AttendanceEventType EventType,
    DateTime OccurredAtUtc);
