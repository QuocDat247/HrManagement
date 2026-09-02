namespace HrManagement.Application.Payroll.Calculations;

public interface IPayrollPolicyProfileSource
{
    PayrollPolicyProfile GetCurrent();
}
