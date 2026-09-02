namespace HrManagement.Application.Payroll.Calculations;

public sealed class PayrollPreviewService
    : IPayrollPreviewService
{
    private readonly IPayrollCalculationInputService
        _inputService;

    private readonly IBaseSalaryProrationPolicy
        _baseSalaryPolicy;

    private readonly IEmployeeOvertimePayabilityResolver
        _overtimeResolver;

    private readonly IOvertimeAmountPolicy
        _overtimeAmountPolicy;

    private readonly IMoneyRoundingPolicy
        _moneyRoundingPolicy;

    public PayrollPreviewService(
        IPayrollCalculationInputService inputService,
        IBaseSalaryProrationPolicy baseSalaryPolicy,
        IEmployeeOvertimePayabilityResolver overtimeResolver,
        IOvertimeAmountPolicy overtimeAmountPolicy,
        IMoneyRoundingPolicy moneyRoundingPolicy)
    {
        _inputService =
            inputService;

        _baseSalaryPolicy =
            baseSalaryPolicy;

        _overtimeResolver =
            overtimeResolver;

        _overtimeAmountPolicy =
            overtimeAmountPolicy;

        _moneyRoundingPolicy =
            moneyRoundingPolicy;
    }

    public async Task<PayrollPreview> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        PayrollCalculationInput input =
            await _inputService
                .GetAsync(
                    year,
                    month,
                    cancellationToken);

        if (!input.IsStructurallyReady)
        {
            return new PayrollPreview(
                year,
                month,
                input.TimesheetPeriodId,
                [],
                input.Issues);
        }

        var employees =
            new List<PayrollEmployeePreview>();

        var allIssues =
            new List<PayrollCalculationIssue>(
                input.Issues);

        foreach (
            PayrollEmployeeCalculationInput employeeInput
            in input.Employees)
        {
            var employeeIssues =
                new List<PayrollCalculationIssue>();

            BaseSalaryProrationResult baseSalary =
                _baseSalaryPolicy.Calculate(
                    year,
                    month,
                    employeeInput.CompensationSegments);

            decimal roundedBaseSalary =
                _moneyRoundingPolicy.Round(
                    baseSalary.TotalAmount,
                    baseSalary.CurrencyCode);

            IReadOnlyList<OvertimePayabilityResolution>
                overtimeResolutions =
                    _overtimeResolver.Resolve(
                        employeeInput);

            int approvedOvertimeMinutes =
                employeeInput
                    .ApprovedOvertime
                    .Sum(
                        overtime =>
                            overtime.ApprovedMinutes);

            bool requiresOvertimeReview =
                overtimeResolutions.Any(
                    resolution =>
                        resolution.Status ==
                            OvertimePayabilityStatus
                                .RequiresReview);

            int? payableOvertimeMinutes;

            if (requiresOvertimeReview)
            {
                payableOvertimeMinutes =
                    null;

                employeeIssues.Add(
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .OvertimeRequiresReview,
                        employeeInput.EmployeeId,
                        "Có yêu cầu tăng ca cần được xem xét trước khi hoàn tất bảng lương."));
            }
            else
            {
                payableOvertimeMinutes =
                    overtimeResolutions.Sum(
                        resolution =>
                            resolution.PayableMinutes
                            ?? 0);
            }

            decimal? overtimeAmount =
                null;

            decimal? grossAmount =
                null;

            if (!requiresOvertimeReview)
            {
                OvertimeAmountCalculationResult
                    overtimeCalculation =
                        _overtimeAmountPolicy.Calculate(
                            year,
                            month,
                            employeeInput,
                            overtimeResolutions);

                if (!overtimeCalculation.IsCalculated
                    || !overtimeCalculation.Amount.HasValue)
                {
                    employeeIssues.Add(
                        new PayrollCalculationIssue(
                            PayrollCalculationIssueCode
                                .OvertimePayRateNotConfigured,
                            employeeInput.EmployeeId,
                            string.IsNullOrWhiteSpace(
                                overtimeCalculation.ErrorMessage)
                                ? "Chưa thể xác định tiền tăng ca."
                                : overtimeCalculation.ErrorMessage));
                }
                else
                {
                    overtimeAmount =
                        _moneyRoundingPolicy.Round(
                            overtimeCalculation.Amount.Value,
                            baseSalary.CurrencyCode);

                    grossAmount =
                        _moneyRoundingPolicy.Round(
                            roundedBaseSalary
                            + overtimeAmount.Value,
                            baseSalary.CurrencyCode);
                }
            }

            allIssues.AddRange(
                employeeIssues);

            employees.Add(
                new PayrollEmployeePreview(
                    employeeInput.EmployeeId,
                    employeeInput.EmployeeCode,
                    employeeInput.EmployeeFullName,
                    baseSalary.CurrencyCode,
                    roundedBaseSalary,
                    approvedOvertimeMinutes,
                    payableOvertimeMinutes,
                    overtimeAmount,
                    grossAmount,
                    overtimeResolutions,
                    employeeIssues));
        }

        return new PayrollPreview(
            year,
            month,
            input.TimesheetPeriodId,
            employees,
            allIssues);
    }
}
