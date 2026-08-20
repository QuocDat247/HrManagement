using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Domain.Attendance.Records;

public sealed class AttendanceRecord
{
    public AttendanceCalculationStatus Status
    {
        get;
        private set;
    } = AttendanceCalculationStatus.NotCalculated;

    public int WorkedMinutes
    {
        get;
        private set;
    }

    public int LateMinutes
    {
        get;
        private set;
    }

    public int EarlyLeaveMinutes
    {
        get;
        private set;
    }

    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public Guid EmploymentPeriodId
    {
        get;
    }

    public Guid WorkScheduleAssignmentId
    {
        get;
    }

    public Guid WorkScheduleId
    {
        get;
    }

    public DateOnly WorkDate
    {
        get;
    }

    public string TimeZoneId
    {
        get;
    }

    public bool IsWorkingDay
    {
        get;
    }

    public TimeOnly? ExpectedStartTime
    {
        get;
    }

    public TimeOnly? ExpectedEndTime
    {
        get;
    }

    public int ExpectedBreakMinutes
    {
        get;
    }

    public int ExpectedPlannedMinutes
    {
        get;
    }

    public bool IsOvernight =>
        IsWorkingDay
        && ExpectedStartTime.HasValue
        && ExpectedEndTime.HasValue
        && ExpectedEndTime.Value <
            ExpectedStartTime.Value;

    public AttendanceRecord(
        Guid id,
        Guid employeeId,
        Guid employmentPeriodId,
        Guid workScheduleAssignmentId,
        Guid workScheduleId,
        DateOnly workDate,
        string timeZoneId,
        bool isWorkingDay,
        TimeOnly? expectedStartTime = null,
        TimeOnly? expectedEndTime = null,
        int expectedBreakMinutes = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã bản ghi chấm công không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (employmentPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giai đoạn làm việc không hợp lệ.",
                nameof(employmentPeriodId));
        }

        if (workScheduleAssignmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã phân lịch làm việc không hợp lệ.",
                nameof(workScheduleAssignmentId));
        }

        if (workScheduleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch làm việc không hợp lệ.",
                nameof(workScheduleId));
        }

        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày chấm công không hợp lệ.",
                nameof(workDate));
        }

        if (string.IsNullOrWhiteSpace(
                timeZoneId))
        {
            throw new ArgumentException(
                "Múi giờ chấm công không được để trống.",
                nameof(timeZoneId));
        }

        if (expectedBreakMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedBreakMinutes),
                "Thời gian nghỉ dự kiến không được âm.");
        }

        if (!isWorkingDay)
        {
            if (expectedStartTime.HasValue
                || expectedEndTime.HasValue
                || expectedBreakMinutes != 0)
            {
                throw new ArgumentException(
                    "Ngày không làm việc không được có giờ làm hoặc thời gian nghỉ dự kiến.");
            }

            Id =
                id;

            EmployeeId =
                employeeId;

            EmploymentPeriodId =
                employmentPeriodId;

            WorkScheduleAssignmentId =
                workScheduleAssignmentId;

            WorkScheduleId =
                workScheduleId;

            WorkDate =
                workDate;

            TimeZoneId =
                timeZoneId.Trim();

            IsWorkingDay =
                false;

            ExpectedStartTime =
                null;

            ExpectedEndTime =
                null;

            ExpectedBreakMinutes =
                0;

            ExpectedPlannedMinutes =
                0;

            return;
        }

        if (!expectedStartTime.HasValue
            || !expectedEndTime.HasValue)
        {
            throw new ArgumentException(
                "Ngày làm việc phải có giờ bắt đầu và giờ kết thúc dự kiến.");
        }

        EnsureMinutePrecision(
            expectedStartTime.Value,
            nameof(expectedStartTime));

        EnsureMinutePrecision(
            expectedEndTime.Value,
            nameof(expectedEndTime));

        if (expectedStartTime.Value ==
            expectedEndTime.Value)
        {
            throw new ArgumentException(
                "Giờ bắt đầu và giờ kết thúc dự kiến không được trùng nhau.");
        }

        int shiftMinutes =
            CalculateShiftMinutes(
                expectedStartTime.Value,
                expectedEndTime.Value);

        if (expectedBreakMinutes >=
            shiftMinutes)
        {
            throw new ArgumentException(
                "Thời gian nghỉ dự kiến phải ngắn hơn thời lượng ca làm việc.",
                nameof(expectedBreakMinutes));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        EmploymentPeriodId =
            employmentPeriodId;

        WorkScheduleAssignmentId =
            workScheduleAssignmentId;

        WorkScheduleId =
            workScheduleId;

        WorkDate =
            workDate;

        TimeZoneId =
            timeZoneId.Trim();

        IsWorkingDay =
            true;

        ExpectedStartTime =
            expectedStartTime;

        ExpectedEndTime =
            expectedEndTime;

        ExpectedBreakMinutes =
            expectedBreakMinutes;

        ExpectedPlannedMinutes =
            shiftMinutes
            - expectedBreakMinutes;
    }

    private static int CalculateShiftMinutes(
        TimeOnly startTime,
        TimeOnly endTime)
    {
        int startMinutes =
            startTime.Hour * 60
            + startTime.Minute;

        int endMinutes =
            endTime.Hour * 60
            + endTime.Minute;

        int result =
            endMinutes
            - startMinutes;

        if (result < 0)
        {
            result +=
                24 * 60;
        }

        return result;
    }

    private static void EnsureMinutePrecision(
        TimeOnly value,
        string parameterName)
    {
        if (value.Ticks %
            TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException(
                "Giờ dự kiến phải có độ chính xác theo phút.",
                parameterName);
        }
    }

    public void ApplyCalculation(
    DailyAttendanceCalculation calculation,
    AttendanceScheduleAdherence adherence)
    {
        ArgumentNullException.ThrowIfNull(
            calculation);

        ArgumentNullException.ThrowIfNull(
            adherence);

        if (calculation.Status ==
            AttendanceCalculationStatus.NotCalculated)
        {
            throw new ArgumentException(
                "Kết quả tính công chưa được tính.",
                nameof(calculation));
        }

        if (!IsWorkingDay
            && calculation.Status !=
                AttendanceCalculationStatus.NonWorkingDay)
        {
            throw new InvalidOperationException(
                "Ngày không làm việc phải có trạng thái NonWorkingDay.");
        }

        if (IsWorkingDay
            && calculation.Status ==
                AttendanceCalculationStatus.NonWorkingDay)
        {
            throw new InvalidOperationException(
                "Ngày làm việc không thể có trạng thái NonWorkingDay.");
        }

        if (calculation.WorkedMinutes < 0)
        {
            throw new InvalidOperationException(
                "Số phút làm việc không được âm.");
        }

        if (adherence.LateMinutes < 0
            || adherence.EarlyLeaveMinutes < 0)
        {
            throw new InvalidOperationException(
                "Số phút vi phạm lịch làm việc không được âm.");
        }

        if (calculation.Status is
                AttendanceCalculationStatus.Absent
                or AttendanceCalculationStatus.NonWorkingDay
            && (
                adherence.LateMinutes != 0
                || adherence.EarlyLeaveMinutes != 0
            ))
        {
            throw new InvalidOperationException(
                "Ngày vắng mặt hoặc không làm việc không được có phút đi trễ hoặc về sớm.");
        }

        Status =
            calculation.Status;

        WorkedMinutes =
            calculation.WorkedMinutes;

        LateMinutes =
            adherence.LateMinutes;

        EarlyLeaveMinutes =
            adherence.EarlyLeaveMinutes;
    }
}
