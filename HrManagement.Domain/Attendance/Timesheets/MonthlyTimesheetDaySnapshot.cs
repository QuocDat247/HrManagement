using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Domain.Attendance.Timesheets;

public sealed class MonthlyTimesheetDaySnapshot
{
    public Guid Id
    {
        get;
    }

    public Guid TimesheetPeriodId
    {
        get;
    }

    public Guid AttendanceRecordId
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public DateOnly WorkDate
    {
        get;
    }

    public bool IsWorkingDay
    {
        get;
    }

    public int ExpectedPlannedMinutes
    {
        get;
    }

    public AttendanceCalculationStatus Status
    {
        get;
    }

    public int WorkedMinutes
    {
        get;
    }

    public int LateMinutes
    {
        get;
    }

    public int EarlyLeaveMinutes
    {
        get;
    }

    public int CorrectionRevision
    {
        get;
    }

    public MonthlyTimesheetDaySnapshot(
        Guid id,
        Guid timesheetPeriodId,
        Guid attendanceRecordId,
        Guid employeeId,
        DateOnly workDate,
        bool isWorkingDay,
        int expectedPlannedMinutes,
        AttendanceCalculationStatus status,
        int workedMinutes,
        int lateMinutes,
        int earlyLeaveMinutes,
        int correctionRevision)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã snapshot bảng công không hợp lệ.",
                nameof(id));
        }

        if (timesheetPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã kỳ công không hợp lệ.",
                nameof(timesheetPeriodId));
        }

        if (attendanceRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã bản ghi chấm công không hợp lệ.",
                nameof(attendanceRecordId));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày bảng công không hợp lệ.",
                nameof(workDate));
        }

        if (!Enum.IsDefined(
                status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "Trạng thái chấm công không hợp lệ.");
        }

        if (status is
            AttendanceCalculationStatus.NotCalculated
            or AttendanceCalculationStatus.Incomplete)
        {
            throw new ArgumentException(
                "Không thể snapshot bản ghi chấm công chưa hoàn tất.",
                nameof(status));
        }

        if (expectedPlannedMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedPlannedMinutes),
                "Số phút dự kiến không được âm.");
        }

        if (workedMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workedMinutes),
                "Số phút làm việc không được âm.");
        }

        if (lateMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lateMinutes),
                "Số phút đi trễ không được âm.");
        }

        if (earlyLeaveMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(earlyLeaveMinutes),
                "Số phút về sớm không được âm.");
        }

        if (correctionRevision < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(correctionRevision),
                "Phiên bản điều chỉnh không được âm.");
        }

        Id =
            id;

        TimesheetPeriodId =
            timesheetPeriodId;

        AttendanceRecordId =
            attendanceRecordId;

        EmployeeId =
            employeeId;

        WorkDate =
            workDate;

        IsWorkingDay =
            isWorkingDay;

        ExpectedPlannedMinutes =
            expectedPlannedMinutes;

        Status =
            status;

        WorkedMinutes =
            workedMinutes;

        LateMinutes =
            lateMinutes;

        EarlyLeaveMinutes =
            earlyLeaveMinutes;

        CorrectionRevision =
            correctionRevision;
    }
}
