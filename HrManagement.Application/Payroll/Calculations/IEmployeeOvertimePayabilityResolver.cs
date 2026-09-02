namespace HrManagement.Application.Payroll.Calculations;

public interface IEmployeeOvertimePayabilityResolver
{
    IReadOnlyList<OvertimePayabilityResolution> Resolve(
        PayrollEmployeeCalculationInput employeeInput);
}
