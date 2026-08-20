using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequest>>
        GetOverlappingByEmployeeAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);
}
