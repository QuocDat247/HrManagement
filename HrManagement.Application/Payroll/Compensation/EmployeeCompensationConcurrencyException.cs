namespace HrManagement.Application.Payroll.Compensation;

public sealed class EmployeeCompensationConcurrencyException
    : InvalidOperationException
{
    public EmployeeCompensationConcurrencyException(
        string message)
        : base(message)
    {
    }

    public EmployeeCompensationConcurrencyException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}
