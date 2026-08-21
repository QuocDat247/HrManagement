using HrManagement.Application.Authentication;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public sealed class LeaveRequestStatusService
    : ILeaveRequestStatusService
{
    private readonly ILeaveRequestRepository
        _leaveRequestRepository;

    private readonly ILeaveRequestStatusTransitionPersistence
        _persistence;

    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly TimeProvider
        _timeProvider;

    public LeaveRequestStatusService(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveRequestStatusTransitionPersistence persistence,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _leaveRequestRepository =
            leaveRequestRepository;

        _persistence =
            persistence;

        _currentUserContext =
            currentUserContext;

        _timeProvider =
            timeProvider;
    }

    public async Task<ChangeLeaveRequestStatusResult>
        ChangeStatusAsync(
            ChangeLeaveRequestStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request.LeaveRequestId == Guid.Empty)
        {
            return Failure(
                "Mã đơn nghỉ phép không hợp lệ.");
        }

        if (request.TargetStatus is not
            LeaveRequestStatus.Approved
            and not LeaveRequestStatus.Rejected
            and not LeaveRequestStatus.Cancelled)
        {
            return Failure(
                "Trạng thái đích của đơn nghỉ phép không hợp lệ.");
        }

        if (!_currentUserContext.IsAuthenticated)
        {
            return Failure(
                "Phiên đăng nhập không hợp lệ.");
        }

        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (currentUser is null)
        {
            return Failure(
                "Không xác định được người đang thực hiện thao tác.");
        }

        LeaveRequest? leaveRequest =
            await _leaveRequestRepository
                .GetByIdAsync(
                    request.LeaveRequestId,
                    cancellationToken);

        if (leaveRequest is null)
        {
            return Failure(
                "Không tìm thấy đơn nghỉ phép.");
        }

        DateTime changedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        LeaveRequestStatusChange statusChange;

        try
        {
            statusChange =
                leaveRequest.TransitionTo(
                    Guid.NewGuid(),
                    request.TargetStatus,
                    changedAtUtc,
                    currentUser.UserId,
                    currentUser.Username,
                    request.Note);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        await _persistence.ApplyAsync(
            statusChange,
            cancellationToken);

        return new ChangeLeaveRequestStatusResult(
            IsSuccessful: true,
            LeaveRequestId:
                leaveRequest.Id,
            Status:
                leaveRequest.Status,
            StatusChangeId:
                statusChange.Id);
    }

    private static ChangeLeaveRequestStatusResult Failure(
        string errorMessage)
    {
        return new ChangeLeaveRequestStatusResult(
            IsSuccessful: false,
            ErrorMessage:
                errorMessage);
    }
}
