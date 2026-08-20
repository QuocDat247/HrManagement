using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Application.Attendance.Calculations;

public sealed record RecalculateAttendanceResult(
    bool IsSuccessful,
    Guid? AttendanceRecordId = null,
    AttendanceCalculationStatus? Status = null,
    int WorkedMinutes = 0,
    int LateMinutes = 0,
    int EarlyLeaveMinutes = 0,
    string? ErrorMessage = null);
