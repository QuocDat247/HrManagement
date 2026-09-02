namespace HrManagement.Application.Payroll.Calculations;

public sealed class UnconfiguredOvertimeAmountPolicy
    : IOvertimeAmountPolicy
{
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

        bool hasUnresolvedOvertime =
            resolutions.Any(
                resolution =>
                    !resolution.IsResolved);

        if (hasUnresolvedOvertime)
        {
            return new OvertimeAmountCalculationResult(
                IsCalculated:
                    false,
                ErrorMessage:
                    "Có tăng ca chưa xác định được số phút được trả.");
        }

        int payableMinutes =
            resolutions.Sum(
                resolution =>
                    resolution.PayableMinutes
                    ?? 0);

        if (payableMinutes == 0)
        {
            return new OvertimeAmountCalculationResult(
                IsCalculated:
                    true,
                Amount:
                    0m);
        }

        return new OvertimeAmountCalculationResult(
            IsCalculated:
                false,
            ErrorMessage:
                "Chưa cấu hình cơ sở đơn giá và hệ số tính tiền tăng ca.");
    }
}
