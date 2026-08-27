using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestStatusTransitionContextSource
{
    Task<OvertimeRequest?> GetByIdAsync(
        Guid overtimeRequestId,
        CancellationToken cancellationToken = default);
}
