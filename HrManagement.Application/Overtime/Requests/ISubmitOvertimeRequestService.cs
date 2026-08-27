namespace HrManagement.Application.Overtime.Requests;

public interface ISubmitOvertimeRequestService
{
    Task<SubmitOvertimeRequestResult> SubmitAsync(
        SubmitOvertimeRequestRequest request,
        CancellationToken cancellationToken = default);
}
