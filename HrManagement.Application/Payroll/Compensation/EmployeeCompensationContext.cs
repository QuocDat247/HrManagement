using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Compensation;

public sealed record EmployeeCompensationContext(
    EmploymentPeriod EmploymentPeriod,
    EmployeeCompensation? CurrentCompensation);
