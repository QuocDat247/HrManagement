namespace HrManagement.Domain.Attendance.Calculations;

public enum AttendanceCalculationStatus
{
    NotCalculated = 0,
    NonWorkingDay = 1,
    Absent = 2,
    Present = 3,
    Incomplete = 4,
    ApprovedLeave = 5
}
