using System.Globalization;
using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Desktop.ViewModels;

public sealed record MonthlyTimesheetRowViewModel(
    Guid AttendanceRecordId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeFullName,
    DateOnly WorkDate,
    string WorkDateText,
    bool IsWorkingDay,
    string StatusText,
    int ExpectedPlannedMinutes,
    int WorkedMinutes,
    int LateMinutes,
    int EarlyLeaveMinutes,
    int CorrectionRevision,
    string CorrectionText,
    bool IsFinalized)
{
    public static MonthlyTimesheetRowViewModel From(
        MonthlyTimesheetDayItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        bool isFinalized =
            item.Status is
                AttendanceCalculationStatus.NonWorkingDay
                or AttendanceCalculationStatus.Absent
                or AttendanceCalculationStatus.Present
                or AttendanceCalculationStatus.ApprovedLeave;

        return new MonthlyTimesheetRowViewModel(
            item.AttendanceRecordId,
            item.EmployeeId,
            item.EmployeeCode,
            item.EmployeeFullName,
            item.WorkDate,
            item.WorkDate.ToString(
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture),
            item.IsWorkingDay,
            GetStatusText(
                item.Status),
            item.ExpectedPlannedMinutes,
            item.WorkedMinutes,
            item.LateMinutes,
            item.EarlyLeaveMinutes,
            item.CorrectionRevision,
            item.CorrectionRevision > 0
                ? $"R{item.CorrectionRevision}"
                : "—",
            isFinalized);
    }

    private static string GetStatusText(
        AttendanceCalculationStatus status)
    {
        return status switch
        {
            AttendanceCalculationStatus.NotCalculated =>
                "Chưa tính",

            AttendanceCalculationStatus.NonWorkingDay =>
                "Ngày nghỉ",

            AttendanceCalculationStatus.Absent =>
                "Vắng mặt",

            AttendanceCalculationStatus.Present =>
                "Có mặt",

            AttendanceCalculationStatus.Incomplete =>
                "Thiếu dữ liệu",

            AttendanceCalculationStatus.ApprovedLeave =>
                "Nghỉ phép đã duyệt",

            _ =>
                "Không xác định"
        };
    }
}
