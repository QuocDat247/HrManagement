using HrManagement.Domain.Leave.Types;

namespace HrManagement.Application.Leave.Types;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken = default);
}
