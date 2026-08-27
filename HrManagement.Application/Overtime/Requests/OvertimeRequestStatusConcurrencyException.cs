namespace HrManagement.Application.Overtime.Requests;

public sealed class OvertimeRequestStatusConcurrencyException
    : Exception
{
    public OvertimeRequestStatusConcurrencyException(
        string message)
        : base(message)
    {
    }

    public OvertimeRequestStatusConcurrencyException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}
