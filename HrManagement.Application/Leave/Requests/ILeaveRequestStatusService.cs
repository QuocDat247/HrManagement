namespace HrManagement.Application.Leave.Requests;

public interface ILeaveRequestStatusService
{
    Task<ChangeLeaveRequestStatusResult> ChangeStatusAsync(
        ChangeLeaveRequestStatusRequest request,
        CancellationToken cancellationToken = default);
}
