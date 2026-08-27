using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestSubmissionPersistence
{
    Task SubmitAsync(
        OvertimeRequest overtimeRequest,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default);
}
