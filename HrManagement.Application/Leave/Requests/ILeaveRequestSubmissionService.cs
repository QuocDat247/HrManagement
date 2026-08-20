namespace HrManagement.Application.Leave.Requests;

public interface ILeaveRequestSubmissionService
{
    Task<SubmitLeaveRequestResult> SubmitAsync(
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default);
}
