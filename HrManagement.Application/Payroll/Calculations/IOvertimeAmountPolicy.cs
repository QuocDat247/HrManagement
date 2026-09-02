namespace HrManagement.Application.Payroll.Calculations;

public interface IOvertimeAmountPolicy
{
    OvertimeAmountCalculationResult Calculate(
        int year,
        int month,
        PayrollEmployeeCalculationInput employeeInput,
        IReadOnlyList<OvertimePayabilityResolution> resolutions);
}
