using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class CalendarDayBaseSalaryProrationPolicy
    : IBaseSalaryProrationPolicy
{
    public BaseSalaryProrationResult Calculate(
        int year,
        int month,
        IReadOnlyList<EmployeeCompensationSegment> segments,
        IReadOnlyCollection<DateOnly> coveredDates)
    {
        ArgumentNullException.ThrowIfNull(
            segments);

        ArgumentNullException.ThrowIfNull(
            coveredDates);

        ValidatePeriod(
            year,
            month);

        if (segments.Count == 0)
        {
            throw new ArgumentException(
                "Không có cấu hình lương để phân bổ.",
                nameof(segments));
        }

        if (coveredDates.Count == 0)
        {
            throw new ArgumentException(
                "Không có ngày làm việc thuộc phạm vi kỳ lương.",
                nameof(coveredDates));
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

        DateOnly[] normalizedCoveredDates =
            coveredDates
                .Distinct()
                .OrderBy(
                    date =>
                        date)
                .ToArray();

        if (normalizedCoveredDates.Any(
                date =>
                    date < periodStart
                    || date > periodEnd))
        {
            throw new ArgumentException(
                "Danh sách ngày được phân bổ chứa ngày nằm ngoài kỳ lương.",
                nameof(coveredDates));
        }

        foreach (
            DateOnly coveredDate
            in normalizedCoveredDates)
        {
            int matchingCount =
                segments.Count(
                    segment =>
                        segment.EffectiveFrom <=
                            coveredDate
                        && (
                            !segment.EffectiveTo.HasValue
                            || segment.EffectiveTo.Value >=
                                coveredDate
                        ));

            if (matchingCount == 0)
            {
                throw new InvalidOperationException(
                    $"Không có cấu hình lương bao phủ ngày {coveredDate:dd/MM/yyyy}.");
            }

            if (matchingCount > 1)
            {
                throw new InvalidOperationException(
                    $"Có nhiều cấu hình lương cùng bao phủ ngày {coveredDate:dd/MM/yyyy}.");
            }
        }

        int periodCalendarDays =
            periodEnd.DayNumber
            - periodStart.DayNumber
            + 1;

        var components =
            new List<BaseSalaryProrationComponent>();

        foreach (
            EmployeeCompensationSegment segment
            in segments
                .OrderBy(
                    segment =>
                        segment.EffectiveFrom)
                .ThenBy(
                    segment =>
                        segment.CompensationId))
        {
            DateOnly[] appliedDates =
                normalizedCoveredDates
                    .Where(
                        date =>
                            segment.EffectiveFrom <=
                                date
                            && (
                                !segment.EffectiveTo.HasValue
                                || segment.EffectiveTo.Value >=
                                    date
                            ))
                    .ToArray();

            if (appliedDates.Length == 0)
            {
                continue;
            }

            decimal proratedAmount =
                segment.MonthlyBaseSalary
                * appliedDates.Length
                / periodCalendarDays;

            components.Add(
                new BaseSalaryProrationComponent(
                    segment.CompensationId,
                    appliedDates[0],
                    appliedDates[^1],
                    appliedDates.Length,
                    periodCalendarDays,
                    segment.MonthlyBaseSalary,
                    proratedAmount));
        }

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
