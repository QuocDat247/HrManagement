using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class ConfiguredOvertimeAmountPolicy
    : IOvertimeAmountPolicy
{
    private readonly IPayrollPolicyProfileSource
        _profileSource;

    public ConfiguredOvertimeAmountPolicy(
        IPayrollPolicyProfileSource profileSource)
    {
        _profileSource =
            profileSource;
    }

    public OvertimeAmountCalculationResult Calculate(
        int year,
        int month,
        PayrollEmployeeCalculationInput employeeInput,
        IReadOnlyList<OvertimePayabilityResolution> resolutions)
    {
        ArgumentNullException.ThrowIfNull(
            employeeInput);

        ArgumentNullException.ThrowIfNull(
            resolutions);

        ValidatePeriod(
            year,
            month);

        if (resolutions.Any(
                resolution =>
                    !resolution.IsResolved))
        {
            return Failure(
                "Có tăng ca chưa xác định được số phút được trả.");
        }

        int totalPayableMinutes =
            resolutions.Sum(
                resolution =>
                    resolution.PayableMinutes
                    ?? 0);

        if (totalPayableMinutes == 0)
        {
            return new OvertimeAmountCalculationResult(
                IsCalculated:
                    true,
                Amount:
                    0m);
        }

        PayrollPolicyProfile profile =
            _profileSource.GetCurrent();

        decimal totalAmount =
            0m;

        foreach (
            OvertimePayabilityResolution resolution
            in resolutions)
        {
            int payableMinutes =
                resolution.PayableMinutes
                ?? 0;

            if (payableMinutes == 0)
            {
                continue;
            }

            if (resolution.EmployeeId !=
                employeeInput.EmployeeId)
            {
                return Failure(
                    "Kết quả tăng ca không thuộc nhân viên đang tính lương.");
            }

            EmployeeCompensationSegment[] matchingSegments =
                employeeInput
                    .CompensationSegments
                    .Where(
                        segment =>
                            segment.EffectiveFrom <=
                                resolution.WorkDate
                            && (
                                !segment.EffectiveTo.HasValue
                                || segment.EffectiveTo.Value >=
                                    resolution.WorkDate
                            ))
                    .ToArray();

            if (matchingSegments.Length == 0)
            {
                return Failure(
                    $"Không có cấu hình lương bao phủ ngày tăng ca {resolution.WorkDate:dd/MM/yyyy}.");
            }

            if (matchingSegments.Length > 1)
            {
                return Failure(
                    $"Có nhiều cấu hình lương cùng bao phủ ngày tăng ca {resolution.WorkDate:dd/MM/yyyy}.");
            }

            EmployeeCompensationSegment compensation =
                matchingSegments[0];

            decimal amount =
                compensation.MonthlyBaseSalary
                / profile.StandardMonthlyWorkingMinutes
                * payableMinutes
                * profile.NonWorkingDayOvertimeMultiplier;

            totalAmount +=
                amount;
        }

        return new OvertimeAmountCalculationResult(
            IsCalculated:
                true,
            Amount:
                totalAmount);
    }

    private static OvertimeAmountCalculationResult Failure(
        string message)
    {
        return new OvertimeAmountCalculationResult(
            IsCalculated:
                false,
            ErrorMessage:
                message);
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
