using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public interface ILeaveRequestSubmissionPersistence
{
    Task SubmitAsync(
        LeaveRequest leaveRequest,
        CancellationToken cancellationToken = default);
}
