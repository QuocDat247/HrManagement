using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestStatusTransitionPersistence
{
    Task ApplyAsync(
        OvertimeRequestStatusChange statusChange,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default);
}
