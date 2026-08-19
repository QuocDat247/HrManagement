namespace HrManagement.Domain.Attendance.Schedules;

public sealed class EmployeeWorkScheduleAssignment
{
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

    public Guid WorkScheduleId
    {
        get;
    }

    public DateOnly EffectiveFrom
    {
        get;
    }

    public DateOnly? EffectiveTo
    {
        get;
        private set;
    }

    public bool IsOpen =>
        EffectiveTo is null;

    public EmployeeWorkScheduleAssignment(
        Guid id,
        Guid employeeId,
        Guid employmentPeriodId,
        Guid workScheduleId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã phân lịch làm việc không hợp lệ.",
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

        if (workScheduleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch làm việc không hợp lệ.",
                nameof(workScheduleId));
        }

        if (effectiveFrom == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu áp dụng lịch làm việc không hợp lệ.",
                nameof(effectiveFrom));
        }

        if (effectiveTo.HasValue
            && effectiveTo.Value <
                effectiveFrom)
        {
            throw new ArgumentException(
                "Ngày kết thúc áp dụng lịch làm việc không thể trước ngày bắt đầu.",
                nameof(effectiveTo));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        EmploymentPeriodId =
            employmentPeriodId;

        WorkScheduleId =
            workScheduleId;

        EffectiveFrom =
            effectiveFrom;

        EffectiveTo =
            effectiveTo;
    }

    public void Close(
        DateOnly effectiveTo)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException(
                "Phân lịch làm việc đã được kết thúc.");
        }

        if (effectiveTo == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc áp dụng lịch làm việc không hợp lệ.",
                nameof(effectiveTo));
        }

        if (effectiveTo <
            EffectiveFrom)
        {
            throw new ArgumentException(
                "Ngày kết thúc áp dụng lịch làm việc không thể trước ngày bắt đầu.",
                nameof(effectiveTo));
        }

        EffectiveTo =
            effectiveTo;
    }

    internal void Reopen()
    {
        if (IsOpen)
        {
            throw new InvalidOperationException(
                "Phân lịch làm việc đang mở.");
        }

        EffectiveTo =
            null;
    }
}
