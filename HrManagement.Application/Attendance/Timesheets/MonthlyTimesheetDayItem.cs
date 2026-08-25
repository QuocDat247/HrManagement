using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed record MonthlyTimesheetDayItem(
    Guid AttendanceRecordId,
    Guid EmployeeId,
    DateOnly WorkDate,
    bool IsWorkingDay,
    int ExpectedPlannedMinutes,
    AttendanceCalculationStatus Status,
    int WorkedMinutes,
    int LateMinutes,
    int EarlyLeaveMinutes,
    int CorrectionRevision);
