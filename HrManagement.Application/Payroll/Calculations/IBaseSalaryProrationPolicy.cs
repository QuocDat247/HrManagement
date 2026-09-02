using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Calculations;

public interface IBaseSalaryProrationPolicy
{
    BaseSalaryProrationResult Calculate(
        int year,
        int month,
        IReadOnlyList<EmployeeCompensationSegment> segments);
}
