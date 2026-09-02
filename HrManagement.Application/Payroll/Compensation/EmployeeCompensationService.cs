using HrManagement.Application.Authentication;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Compensation;

public sealed class EmployeeCompensationService
    : IEmployeeCompensationService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmployeeCompensationContextSource
        _contextSource;

    private readonly IEmployeeCompensationPersistence
        _persistence;

    private readonly IEmployeeCompensationAuthorizationPolicy
        _authorizationPolicy;

    private readonly ICurrentUserContext
        _currentUserContext;

    public EmployeeCompensationService(
        IEmployeeRepository employeeRepository,
        IEmployeeCompensationContextSource contextSource,
        IEmployeeCompensationPersistence persistence,
        IEmployeeCompensationAuthorizationPolicy authorizationPolicy,
        ICurrentUserContext currentUserContext)
    {
        _employeeRepository =
            employeeRepository;

        _contextSource =
            contextSource;

        _persistence =
            persistence;

        _authorizationPolicy =
            authorizationPolicy;

        _currentUserContext =
            currentUserContext;
    }

    public async Task<SetEmployeeCompensationResult> SetAsync(
        SetEmployeeCompensationRequest request,
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
                "Không thể cập nhật lương khi chưa có người dùng đăng nhập.");
        }

        bool isAuthorized =
            await _authorizationPolicy
                .CanSetAsync(
                    new EmployeeCompensationAuthorizationRequest(
                        currentUser,
                        request.EmployeeId,
                        request.EffectiveFrom),
                    cancellationToken);

        if (!isAuthorized)
        {
            return Failure(
                "Bạn không có quyền cập nhật cấu hình lương.");
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

        EmployeeCompensationContext? context =
            await _contextSource
                .GetAsync(
                    employee.Id,
                    request.EffectiveFrom,
                    cancellationToken);

        if (context is null)
        {
            return Failure(
                "Ngày hiệu lực lương không nằm trong giai đoạn làm việc của nhân viên.");
        }

        EmploymentPeriod employmentPeriod =
            context.EmploymentPeriod;

        if (employmentPeriod.EmployeeId !=
            employee.Id)
        {
            return Failure(
                "Giai đoạn làm việc không thuộc nhân viên.");
        }

        if (request.EffectiveFrom <
                employmentPeriod.StartDate
            || (
                employmentPeriod.EndDate.HasValue
                && request.EffectiveFrom >
                    employmentPeriod.EndDate.Value
            ))
        {
            return Failure(
                "Ngày hiệu lực lương không nằm trong giai đoạn làm việc của nhân viên.");
        }

        EmployeeCompensation? currentCompensation =
            context.CurrentCompensation;

        if (currentCompensation is not null)
        {
            if (currentCompensation.EmployeeId !=
                    employee.Id
                || currentCompensation.EmploymentPeriodId !=
                    employmentPeriod.Id)
            {
                return Failure(
                    "Cấu hình lương hiện tại không khớp với giai đoạn làm việc.");
            }

            if (!currentCompensation.IsOpen)
            {
                return Failure(
                    "Cấu hình lương hiện tại không ở trạng thái mở.");
            }

            if (request.EffectiveFrom <=
                currentCompensation.EffectiveFrom)
            {
                return Failure(
                    "Ngày hiệu lực lương mới phải sau ngày bắt đầu của cấu hình lương hiện tại.");
            }

            DateOnly closeDate =
                request.EffectiveFrom
                    .AddDays(
                        -1);

            try
            {
                currentCompensation.Close(
                    closeDate);
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

        EmployeeCompensation newCompensation;

        try
        {
            newCompensation =
                new EmployeeCompensation(
                    Guid.NewGuid(),
                    employee.Id,
                    employmentPeriod.Id,
                    request.EffectiveFrom,
                    request.MonthlyBaseSalary,
                    request.CurrencyCode);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                CleanArgumentMessage(
                    exception));
        }

        try
        {
            await _persistence
                .ApplyAsync(
                    currentCompensation,
                    newCompensation,
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

        return new SetEmployeeCompensationResult(
            IsSuccessful:
                true,
            CompensationId:
                newCompensation.Id,
            PreviousCompensationId:
                currentCompensation?.Id);
    }

    private static string? ValidateRequest(
        SetEmployeeCompensationRequest request)
    {
        if (request.EmployeeId ==
            Guid.Empty)
        {
            return
                "Mã nhân viên không hợp lệ.";
        }

        if (request.EffectiveFrom ==
            default)
        {
            return
                "Ngày bắt đầu hiệu lực lương không hợp lệ.";
        }

        if (request.MonthlyBaseSalary < 0)
        {
            return
                "Lương cơ bản tháng không được âm.";
        }

        if (string.IsNullOrWhiteSpace(
                request.CurrencyCode))
        {
            return
                "Mã tiền tệ là bắt buộc.";
        }

        string normalizedCurrencyCode =
            request.CurrencyCode
                .Trim()
                .ToUpperInvariant();

        if (normalizedCurrencyCode.Length != 3
            || normalizedCurrencyCode.Any(
                character =>
                    character < 'A'
                    || character > 'Z'))
        {
            return
                "Mã tiền tệ phải gồm đúng 3 chữ cái ASCII.";
        }

        return null;
    }

    private static string CleanArgumentMessage(
        ArgumentException exception)
    {
        if (!string.IsNullOrWhiteSpace(
                exception.ParamName))
        {
            int parameterMarkerIndex =
                exception.Message.IndexOf(
                    " (Parameter '",
                    StringComparison.Ordinal);

            if (parameterMarkerIndex >= 0)
            {
                return exception.Message[
                    ..parameterMarkerIndex];
            }
        }

        return exception.Message;
    }

    private static SetEmployeeCompensationResult Failure(
        string errorMessage)
    {
        return new SetEmployeeCompensationResult(
            IsSuccessful:
                false,
            ErrorMessage:
                errorMessage);
    }
}
