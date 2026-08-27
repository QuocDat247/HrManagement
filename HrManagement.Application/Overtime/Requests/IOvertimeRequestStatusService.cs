namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestStatusService
{
    Task<ChangeOvertimeRequestStatusResult> ChangeStatusAsync(
        ChangeOvertimeRequestStatusRequest request,
        CancellationToken cancellationToken = default);
}
