using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public interface ILeaveRequestStatusTransitionPersistence
{
    Task ApplyAsync(
        LeaveRequestStatusChange statusChange,
        CancellationToken cancellationToken = default);
}
