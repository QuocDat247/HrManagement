using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class CalendarDayBaseSalaryProrationPolicy
    : IBaseSalaryProrationPolicy
{
    public BaseSalaryProrationResult Calculate(
        int year,
        int month,
        IReadOnlyList<EmployeeCompensationSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(
            segments);

        ValidatePeriod(
            year,
            month);

        if (segments.Count == 0)
        {
            throw new ArgumentException(
                "Không có cấu hình lương để phân bổ.",
                nameof(segments));
        }

        Guid[] employeeIds =
            segments
                .Select(
                    segment =>
                        segment.EmployeeId)
                .Distinct()
                .ToArray();

        if (employeeIds.Length != 1)
        {
            throw new ArgumentException(
                "Các cấu hình lương phải thuộc cùng một nhân viên.",
                nameof(segments));
        }

        string[] currencyCodes =
            segments
                .Select(
                    segment =>
                        segment.CurrencyCode)
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();

        if (currencyCodes.Length != 1)
        {
            throw new ArgumentException(
                "Không thể phân bổ lương cơ bản với nhiều loại tiền tệ.",
                nameof(segments));
        }

        DateOnly periodStart =
            new(
                year,
                month,
                1);

        DateOnly periodEnd =
            new(
                year,
                month,
                DateTime.DaysInMonth(
                    year,
                    month));

        int periodCalendarDays =
            periodEnd.DayNumber
            - periodStart.DayNumber
            + 1;

        var appliedSegments =
            segments
                .Select(
                    segment =>
                    {
                        DateOnly appliedFrom =
                            segment.EffectiveFrom >
                                periodStart
                                ? segment.EffectiveFrom
                                : periodStart;

                        DateOnly segmentEnd =
                            segment.EffectiveTo
                                ?? periodEnd;

                        DateOnly appliedTo =
                            segmentEnd <
                                periodEnd
                                ? segmentEnd
                                : periodEnd;

                        return new
                        {
                            Segment =
                                segment,
                            AppliedFrom =
                                appliedFrom,
                            AppliedTo =
                                appliedTo
                        };
                    })
                .Where(
                    item =>
                        item.AppliedFrom <=
                            item.AppliedTo)
                .OrderBy(
                    item =>
                        item.AppliedFrom)
                .ThenBy(
                    item =>
                        item.Segment.CompensationId)
                .ToArray();

        if (appliedSegments.Length == 0)
        {
            throw new ArgumentException(
                "Không có cấu hình lương nào giao với kỳ cần phân bổ.",
                nameof(segments));
        }

        for (
            int index = 1;
            index < appliedSegments.Length;
            index++)
        {
            if (appliedSegments[index].AppliedFrom <=
                appliedSegments[index - 1].AppliedTo)
            {
                throw new InvalidOperationException(
                    "Các khoảng hiệu lực lương bị chồng lấn trong kỳ.");
            }
        }

        BaseSalaryProrationComponent[] components =
            appliedSegments
                .Select(
                    item =>
                    {
                        int coveredCalendarDays =
                            item.AppliedTo.DayNumber
                            - item.AppliedFrom.DayNumber
                            + 1;

                        decimal proratedAmount =
                            item.Segment.MonthlyBaseSalary
                            * coveredCalendarDays
                            / periodCalendarDays;

                        return new BaseSalaryProrationComponent(
                            item.Segment.CompensationId,
                            item.AppliedFrom,
                            item.AppliedTo,
                            coveredCalendarDays,
                            periodCalendarDays,
                            item.Segment.MonthlyBaseSalary,
                            proratedAmount);
                    })
                .ToArray();

        decimal totalAmount =
            components.Sum(
                component =>
                    component.ProratedAmount);

        return new BaseSalaryProrationResult(
            currencyCodes[0],
            totalAmount,
            components);
    }

    private static void ValidatePeriod(
        int year,
        int month)
    {
        if (year < 2000
            || year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Năm kỳ lương không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ lương phải từ 1 đến 12.");
        }
    }
}
