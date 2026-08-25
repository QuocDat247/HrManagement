using HrManagement.Application.Authentication;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed class CloseTimesheetPeriodService
    : ICloseTimesheetPeriodService
{
    private readonly ICloseTimesheetPeriodPersistence
        _persistence;

    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly ITimesheetPeriodClosingAuthorizationPolicy
        _authorizationPolicy;

    private readonly TimeProvider
        _timeProvider;

    public CloseTimesheetPeriodService(
        ICloseTimesheetPeriodPersistence persistence,
        ICurrentUserContext currentUserContext,
        ITimesheetPeriodClosingAuthorizationPolicy authorizationPolicy,
        TimeProvider timeProvider)
    {
        _persistence =
            persistence;

        _currentUserContext =
            currentUserContext;

        _authorizationPolicy =
            authorizationPolicy;

        _timeProvider =
            timeProvider;
    }

    public async Task<CloseTimesheetPeriodResult> CloseAsync(
        CloseTimesheetPeriodRequest request,
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
                "Không thể đóng kỳ công khi chưa có người dùng đăng nhập.");
        }

        bool isAuthorized =
            await _authorizationPolicy
                .CanCloseAsync(
                    new TimesheetPeriodClosingAuthorizationRequest(
                        currentUser,
                        request.Year,
                        request.Month),
                    cancellationToken);

        if (!isAuthorized)
        {
            return Failure(
                "Bạn không có quyền đóng kỳ công.");
        }

        DateTime closedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        try
        {
            CloseTimesheetPeriodPersistenceResult
                persistenceResult =
                    await _persistence
                        .CloseAsync(
                            request.Year,
                            request.Month,
                            closedAtUtc,
                            currentUser.UserId,
                            currentUser.Username,
                            cancellationToken);

            return new CloseTimesheetPeriodResult(
                IsSuccessful: true,
                TimesheetPeriodId:
                    persistenceResult.TimesheetPeriodId,
                SnapshotCount:
                    persistenceResult.SnapshotCount,
                ClosedAtUtc:
                    closedAtUtc);
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
    }

    private static string? ValidateRequest(
        CloseTimesheetPeriodRequest request)
    {
        if (request.Year < 2000
            || request.Year > 9999)
        {
            return
                "Năm kỳ công không hợp lệ.";
        }

        if (request.Month < 1
            || request.Month > 12)
        {
            return
                "Tháng kỳ công phải từ 1 đến 12.";
        }

        return null;
    }

    private static CloseTimesheetPeriodResult Failure(
        string errorMessage)
    {
        return new CloseTimesheetPeriodResult(
            IsSuccessful: false,
            ErrorMessage:
                errorMessage);
    }
}
