using HrManagement.Domain.Employees;

namespace HrManagement.Domain.Attendance.Schedules;

public static class EmployeeWorkScheduleAssignmentTimelinePolicy
{
    public static void EnsureWithinEmploymentPeriod(
        EmployeeWorkScheduleAssignment assignment,
        EmploymentPeriod employmentPeriod)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        ArgumentNullException.ThrowIfNull(
            employmentPeriod);

        if (assignment.EmployeeId !=
            employmentPeriod.EmployeeId)
        {
            throw new InvalidOperationException(
                "Phân lịch làm việc không thuộc đúng nhân viên của giai đoạn làm việc.");
        }

        if (assignment.EmploymentPeriodId !=
            employmentPeriod.Id)
        {
            throw new InvalidOperationException(
                "Phân lịch làm việc không thuộc đúng giai đoạn làm việc.");
        }

        if (assignment.EffectiveFrom <
            employmentPeriod.StartDate)
        {
            throw new InvalidOperationException(
                "Ngày bắt đầu áp dụng lịch làm việc không thể trước ngày bắt đầu giai đoạn làm việc.");
        }

        if (!employmentPeriod.EndDate.HasValue)
        {
            return;
        }

        if (!assignment.EffectiveTo.HasValue)
        {
            throw new InvalidOperationException(
                "Phân lịch làm việc không thể để mở khi giai đoạn làm việc đã kết thúc.");
        }

        if (assignment.EffectiveTo.Value >
            employmentPeriod.EndDate.Value)
        {
            throw new InvalidOperationException(
                "Ngày kết thúc áp dụng lịch làm việc không thể sau ngày kết thúc giai đoạn làm việc.");
        }
    }

    public static bool Overlaps(
        EmployeeWorkScheduleAssignment left,
        EmployeeWorkScheduleAssignment right)
    {
        ArgumentNullException.ThrowIfNull(
            left);

        ArgumentNullException.ThrowIfNull(
            right);

        if (left.EmployeeId !=
                right.EmployeeId
            || left.EmploymentPeriodId !=
                right.EmploymentPeriodId)
        {
            return false;
        }

        DateOnly leftEnd =
            left.EffectiveTo
            ?? DateOnly.MaxValue;

        DateOnly rightEnd =
            right.EffectiveTo
            ?? DateOnly.MaxValue;

        return left.EffectiveFrom <=
                   rightEnd
               && right.EffectiveFrom <=
                   leftEnd;
    }

    public static void EnsureNoOverlap(
        EmployeeWorkScheduleAssignment candidate,
        IEnumerable<EmployeeWorkScheduleAssignment>
            existingAssignments)
    {
        ArgumentNullException.ThrowIfNull(
            candidate);

        ArgumentNullException.ThrowIfNull(
            existingAssignments);

        foreach (EmployeeWorkScheduleAssignment existing
                 in existingAssignments)
        {
            if (existing.Id ==
                candidate.Id)
            {
                continue;
            }

            if (Overlaps(
                    candidate,
                    existing))
            {
                throw new InvalidOperationException(
                    "Khoảng thời gian phân lịch làm việc bị chồng lấn.");
            }
        }
    }

    public static DateOnly CalculatePreviousEffectiveTo(
        EmployeeWorkScheduleAssignment currentAssignment,
        DateOnly newEffectiveFrom)
    {
        ArgumentNullException.ThrowIfNull(
            currentAssignment);

        if (!currentAssignment.IsOpen)
        {
            throw new InvalidOperationException(
                "Chỉ có thể chuyển lịch từ một phân lịch đang mở.");
        }

        if (newEffectiveFrom == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu lịch mới không hợp lệ.",
                nameof(newEffectiveFrom));
        }

        if (newEffectiveFrom <=
            currentAssignment.EffectiveFrom)
        {
            throw new ArgumentException(
                "Ngày bắt đầu lịch mới phải sau ngày bắt đầu của phân lịch hiện tại.",
                nameof(newEffectiveFrom));
        }

        return newEffectiveFrom.AddDays(
            -1);
    }
}
