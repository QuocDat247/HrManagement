using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public interface ILeaveRequestStatusHistoryRepository
{
    Task<IReadOnlyList<LeaveRequestStatusChange>>
        GetByLeaveRequestIdAsync(
            Guid leaveRequestId,
            CancellationToken cancellationToken = default);
}
