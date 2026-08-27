using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Authentication;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed class OvertimeRequestStatusService
    : IOvertimeRequestStatusService
{
    private readonly IOvertimeRequestStatusTransitionContextSource
        _contextSource;

    private readonly IOvertimeRequestStatusTransitionPersistence
        _persistence;

    private readonly IOvertimeRequestStatusAuthorizationPolicy
        _authorizationPolicy;

    private readonly IAttendancePeriodLockPolicy
        _periodLockPolicy;

    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly TimeProvider
        _timeProvider;

    public OvertimeRequestStatusService(
        IOvertimeRequestStatusTransitionContextSource contextSource,
        IOvertimeRequestStatusTransitionPersistence persistence,
        IOvertimeRequestStatusAuthorizationPolicy authorizationPolicy,
        IAttendancePeriodLockPolicy periodLockPolicy,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _contextSource =
            contextSource;

        _persistence =
            persistence;

        _authorizationPolicy =
            authorizationPolicy;

        _periodLockPolicy =
            periodLockPolicy;

        _currentUserContext =
            currentUserContext;

        _timeProvider =
            timeProvider;
    }

    public async Task<ChangeOvertimeRequestStatusResult>
        ChangeStatusAsync(
            ChangeOvertimeRequestStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        string? validationError =
            ValidateRequest(
                request);

        if (validationError is not null)
        {
            return Failure(
                validationError);
        }

        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (currentUser is null)
        {
            return Failure(
                "Không thể thay đổi trạng thái tăng ca khi chưa có người dùng đăng nhập.");
        }

        bool isAuthorized =
            await _authorizationPolicy
                .CanChangeStatusAsync(
                    new OvertimeRequestStatusAuthorizationRequest(
                        currentUser,
                        request.OvertimeRequestId,
                        request.TargetStatus),
                    cancellationToken);

        if (!isAuthorized)
        {
            return Failure(
                "Bạn không có quyền thay đổi trạng thái yêu cầu tăng ca.");
        }

        OvertimeRequest? overtimeRequest =
            await _contextSource
                .GetByIdAsync(
                    request.OvertimeRequestId,
                    cancellationToken);

        if (overtimeRequest is null)
        {
            return Failure(
                "Không tìm thấy yêu cầu tăng ca.");
        }

        if (overtimeRequest.Status !=
            request.ExpectedStatus)
        {
            return Failure(
                "Yêu cầu tăng ca đã thay đổi trạng thái. Vui lòng làm mới dữ liệu trước khi thao tác.");
        }

        bool isPeriodLocked =
            await _periodLockPolicy
                .IsLockedAsync(
                    overtimeRequest.WorkDate,
                    cancellationToken);

        if (isPeriodLocked)
        {
            return Failure(
                "Kỳ công của ngày tăng ca đã được đóng. Không thể thay đổi trạng thái yêu cầu tăng ca.");
        }

        DateTime changedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        OvertimeRequestStatusChange statusChange;

        try
        {
            statusChange =
                overtimeRequest.TransitionTo(
                    Guid.NewGuid(),
                    request.TargetStatus,
                    changedAtUtc,
                    currentUser.UserId,
                    currentUser.Username,
                    request.ApprovedMinutes,
                    request.Note);
        }
        catch (ArgumentOutOfRangeException exception)
            when (string.Equals(
                exception.ParamName,
                "approvedMinutes",
                StringComparison.Ordinal))
        {
            return Failure(
                "Số phút tăng ca được duyệt phải từ 1 đến số phút đã yêu cầu.");
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

        try
        {
            await _persistence
                .ApplyAsync(
                    statusChange,
                    currentUser.UserId,
                    currentUser.Username,
                    cancellationToken);
        }
        catch (OvertimeRequestStatusConcurrencyException exception)
        {
            return Failure(
                exception.Message);
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

        return new ChangeOvertimeRequestStatusResult(
            IsSuccessful:
                true,
            OvertimeRequestId:
                overtimeRequest.Id,
            Status:
                overtimeRequest.Status,
            ApprovedMinutes:
                overtimeRequest.ApprovedMinutes,
            StatusChangeId:
                statusChange.Id);
    }

    private static string? ValidateRequest(
        ChangeOvertimeRequestStatusRequest request)
    {
        if (request.OvertimeRequestId == Guid.Empty)
        {
            return
                "Mã yêu cầu tăng ca không hợp lệ.";
        }

        if (!Enum.IsDefined(
                request.ExpectedStatus))
        {
            return
                "Trạng thái hiện tại dự kiến của yêu cầu tăng ca không hợp lệ.";
        }

        if (request.TargetStatus is not
            OvertimeRequestStatus.Approved
            and not OvertimeRequestStatus.Rejected
            and not OvertimeRequestStatus.Cancelled)
        {
            return
                "Trạng thái đích của yêu cầu tăng ca không hợp lệ.";
        }

        return null;
    }

    private static ChangeOvertimeRequestStatusResult Failure(
        string errorMessage)
    {
        return new ChangeOvertimeRequestStatusResult(
            IsSuccessful:
                false,
            ErrorMessage:
                errorMessage);
    }
}
