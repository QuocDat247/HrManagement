using HrManagement.Application.Authentication;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;

namespace HrManagement.Application.Payroll.Periods;

public sealed class ClosePayrollPeriodService
    : IClosePayrollPeriodService
{
    private readonly IPayrollPreviewService
        _previewService;

    private readonly IClosePayrollPeriodPersistence
        _persistence;

    private readonly IPayrollPeriodClosingAuthorizationPolicy
        _authorizationPolicy;

    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly TimeProvider
        _timeProvider;

    public ClosePayrollPeriodService(
        IPayrollPreviewService previewService,
        IClosePayrollPeriodPersistence persistence,
        IPayrollPeriodClosingAuthorizationPolicy authorizationPolicy,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _previewService =
            previewService;

        _persistence =
            persistence;

        _authorizationPolicy =
            authorizationPolicy;

        _currentUserContext =
            currentUserContext;

        _timeProvider =
            timeProvider;
    }

    public async Task<ClosePayrollPeriodResult> CloseAsync(
        ClosePayrollPeriodRequest request,
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
                "Không thể đóng kỳ lương khi chưa có người dùng đăng nhập.");
        }

        bool isAuthorized =
            await _authorizationPolicy
                .CanCloseAsync(
                    new PayrollPeriodClosingAuthorizationRequest(
                        currentUser,
                        request.Year,
                        request.Month),
                    cancellationToken);

        if (!isAuthorized)
        {
            return Failure(
                "Bạn không có quyền đóng kỳ lương.");
        }

        PayrollPreview preview;

        try
        {
            preview =
                await _previewService
                    .GetAsync(
                        request.Year,
                        request.Month,
                        cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                CleanArgumentMessage(
                    exception));
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        if (preview.Year !=
                request.Year
            || preview.Month !=
                request.Month)
        {
            return Failure(
                "Dữ liệu xem trước bảng lương không khớp với kỳ cần đóng.");
        }

        if (!preview.IsFinalizable
            || !preview.TimesheetPeriodId.HasValue)
        {
            string errorMessage =
                preview.Issues
                    .FirstOrDefault()
                    ?.Message
                ?? "Bảng lương chưa đủ điều kiện để đóng.";

            return Failure(
                errorMessage);
        }

        Guid payrollPeriodId =
            Guid.NewGuid();

        DateTime closedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        var payrollPeriod =
            new PayrollPeriod(
                payrollPeriodId,
                preview.TimesheetPeriodId.Value,
                request.Year,
                request.Month);

        var snapshots =
            new List<PayrollEmployeeSnapshot>(
                preview.Employees.Count);

        var employeeIds =
            new HashSet<Guid>();

        foreach (
            PayrollEmployeePreview employee
            in preview.Employees)
        {
            if (!employeeIds.Add(
                    employee.EmployeeId))
            {
                return Failure(
                    "Bảng lương xem trước chứa trùng nhân viên.");
            }

            if (!employee.IsFinalizable
                || !employee.PayableOvertimeMinutes.HasValue
                || !employee.OvertimeAmount.HasValue
                || !employee.GrossAmount.HasValue)
            {
                return Failure(
                    $"Bảng lương của nhân viên {employee.EmployeeCode} chưa đủ điều kiện để đóng.");
            }

            try
            {
                snapshots.Add(
                    new PayrollEmployeeSnapshot(
                        Guid.NewGuid(),
                        payrollPeriodId,
                        employee.EmployeeId,
                        employee.EmployeeCode,
                        employee.EmployeeFullName,
                        employee.CurrencyCode,
                        employee.BaseSalaryAmount,
                        employee.ApprovedOvertimeMinutes,
                        employee.PayableOvertimeMinutes.Value,
                        employee.OvertimeAmount.Value,
                        employee.GrossAmount.Value));
            }
            catch (ArgumentException exception)
            {
                return Failure(
                    CleanArgumentMessage(
                        exception));
            }
        }

        try
        {
            payrollPeriod.Close(
                closedAtUtc,
                currentUser.UserId,
                currentUser.Username);

            await _persistence
                .PersistAsync(
                    payrollPeriod,
                    snapshots,
                    currentUser.UserId,
                    currentUser.Username,
                    cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                CleanArgumentMessage(
                    exception));
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        return new ClosePayrollPeriodResult(
            IsSuccessful:
                true,
            PayrollPeriodId:
                payrollPeriod.Id,
            SnapshotCount:
                snapshots.Count,
            ClosedAtUtc:
                closedAtUtc);
    }

    private static string? ValidateRequest(
        ClosePayrollPeriodRequest request)
    {
        if (request.Year < 2000
            || request.Year > 9999)
        {
            return
                "Năm kỳ lương không hợp lệ.";
        }

        if (request.Month < 1
            || request.Month > 12)
        {
            return
                "Tháng kỳ lương phải từ 1 đến 12.";
        }

        return null;
    }

    private static ClosePayrollPeriodResult Failure(
        string errorMessage)
    {
        return new ClosePayrollPeriodResult(
            IsSuccessful:
                false,
            ErrorMessage:
                errorMessage);
    }

    private static string CleanArgumentMessage(
        ArgumentException exception)
    {
        if (!string.IsNullOrWhiteSpace(
                exception.ParamName))
        {
            int markerIndex =
                exception.Message.IndexOf(
                    " (Parameter '",
                    StringComparison.Ordinal);

            if (markerIndex >= 0)
            {
                return exception.Message[
                    ..markerIndex];
            }
        }

        return exception.Message;
    }
}
