using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Authentication;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed class SubmitOvertimeRequestService
    : ISubmitOvertimeRequestService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IOvertimeRequestSubmissionContextSource
        _contextSource;

    private readonly IAttendancePeriodLockPolicy
        _periodLockPolicy;

    private readonly IOvertimeRequestSubmissionAuthorizationPolicy
        _authorizationPolicy;

    private readonly IOvertimeRequestSubmissionPersistence
        _persistence;

    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly TimeProvider
        _timeProvider;

    public SubmitOvertimeRequestService(
        IEmployeeRepository employeeRepository,
        IOvertimeRequestSubmissionContextSource contextSource,
        IAttendancePeriodLockPolicy periodLockPolicy,
        IOvertimeRequestSubmissionAuthorizationPolicy authorizationPolicy,
        IOvertimeRequestSubmissionPersistence persistence,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _employeeRepository =
            employeeRepository;

        _contextSource =
            contextSource;

        _periodLockPolicy =
            periodLockPolicy;

        _authorizationPolicy =
            authorizationPolicy;

        _persistence =
            persistence;

        _currentUserContext =
            currentUserContext;

        _timeProvider =
            timeProvider;
    }

    public async Task<SubmitOvertimeRequestResult> SubmitAsync(
        SubmitOvertimeRequestRequest request,
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
                "Không thể gửi yêu cầu tăng ca khi chưa có người dùng đăng nhập.");
        }

        bool isAuthorized =
            await _authorizationPolicy
                .CanSubmitAsync(
                    new OvertimeRequestSubmissionAuthorizationRequest(
                        currentUser,
                        request.EmployeeId,
                        request.WorkDate),
                    cancellationToken);

        if (!isAuthorized)
        {
            return Failure(
                "Bạn không có quyền gửi yêu cầu tăng ca.");
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Không tìm thấy nhân viên.");
        }

        EmploymentPeriod? employmentPeriod =
            await _contextSource
                .GetEmploymentPeriodAsync(
                    employee.Id,
                    request.WorkDate,
                    cancellationToken);

        if (employmentPeriod is null)
        {
            return Failure(
                "Ngày tăng ca không nằm trong giai đoạn làm việc của nhân viên.");
        }

        if (employmentPeriod.EmployeeId !=
            employee.Id)
        {
            return Failure(
                "Giai đoạn làm việc không thuộc nhân viên.");
        }

        if (request.WorkDate <
                employmentPeriod.StartDate
            || (
                employmentPeriod.EndDate.HasValue
                && request.WorkDate >
                    employmentPeriod.EndDate.Value
            ))
        {
            return Failure(
                "Ngày tăng ca không nằm trong giai đoạn làm việc của nhân viên.");
        }

        bool isPeriodLocked =
            await _periodLockPolicy
                .IsLockedAsync(
                    request.WorkDate,
                    cancellationToken);

        if (isPeriodLocked)
        {
            return Failure(
                "Kỳ công của ngày tăng ca đã được đóng. Không thể gửi yêu cầu tăng ca.");
        }

        DateTime submittedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        OvertimeRequest overtimeRequest;

        try
        {
            overtimeRequest =
                new OvertimeRequest(
                    Guid.NewGuid(),
                    employee.Id,
                    employmentPeriod.Id,
                    request.WorkDate,
                    request.RequestedMinutes,
                    request.Reason,
                    submittedAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        try
        {
            await _persistence
                .SubmitAsync(
                    overtimeRequest,
                    currentUser.UserId,
                    currentUser.Username,
                    cancellationToken);
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

        return new SubmitOvertimeRequestResult(
            IsSuccessful:
                true,
            OvertimeRequestId:
                overtimeRequest.Id,
            Status:
                overtimeRequest.Status);
    }

    private static string? ValidateRequest(
        SubmitOvertimeRequestRequest request)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            return
                "Mã nhân viên không hợp lệ.";
        }

        if (request.WorkDate == default)
        {
            return
                "Ngày tăng ca không hợp lệ.";
        }

        if (request.RequestedMinutes is <= 0 or > 1440)
        {
            return
                "Số phút tăng ca yêu cầu phải từ 1 đến 1440 phút.";
        }

        string? normalizedReason =
            string.IsNullOrWhiteSpace(
                request.Reason)
                ? null
                : request.Reason.Trim();

        if (normalizedReason?.Length > 500)
        {
            return
                "Lý do tăng ca không được vượt quá 500 ký tự.";
        }

        return null;
    }

    private static SubmitOvertimeRequestResult Failure(
        string errorMessage)
    {
        return new SubmitOvertimeRequestResult(
            IsSuccessful:
                false,
            ErrorMessage:
                errorMessage);
    }
}
