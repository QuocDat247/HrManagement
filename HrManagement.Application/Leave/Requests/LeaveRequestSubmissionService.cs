using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Leave.Types;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Policies;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;

namespace HrManagement.Application.Leave.Requests;

public sealed class LeaveRequestSubmissionService
    : ILeaveRequestSubmissionService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmploymentHistoryRepository
        _employmentHistoryRepository;

    private readonly ILeaveTypeRepository
        _leaveTypeRepository;

    private readonly ILeaveRequestRepository
        _leaveRequestRepository;

    private readonly ILeaveRequestSubmissionPersistence
        _persistence;

    private readonly TimeProvider
        _timeProvider;

    public LeaveRequestSubmissionService(
        IEmployeeRepository employeeRepository,
        IEmploymentHistoryRepository employmentHistoryRepository,
        ILeaveTypeRepository leaveTypeRepository,
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveRequestSubmissionPersistence persistence,
        TimeProvider timeProvider)
    {
        _employeeRepository =
            employeeRepository;

        _employmentHistoryRepository =
            employmentHistoryRepository;

        _leaveTypeRepository =
            leaveTypeRepository;

        _leaveRequestRepository =
            leaveRequestRepository;

        _persistence =
            persistence;

        _timeProvider =
            timeProvider;
    }

    public async Task<SubmitLeaveRequestResult> SubmitAsync(
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (request.LeaveTypeId == Guid.Empty)
        {
            return Failure(
                "Vui lòng chọn loại nghỉ phép.");
        }

        if (request.StartDate == default)
        {
            return Failure(
                "Ngày bắt đầu nghỉ không hợp lệ.");
        }

        if (request.EndDate == default)
        {
            return Failure(
                "Ngày kết thúc nghỉ không hợp lệ.");
        }

        if (request.EndDate <
            request.StartDate)
        {
            return Failure(
                "Ngày kết thúc nghỉ không thể trước ngày bắt đầu nghỉ.");
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

        LeaveType? leaveType =
            await _leaveTypeRepository
                .GetByIdAsync(
                    request.LeaveTypeId,
                    cancellationToken);

        if (leaveType is null)
        {
            return Failure(
                "Không tìm thấy loại nghỉ phép.");
        }

        EmploymentHistory employmentHistory =
            await _employmentHistoryRepository
                .GetByEmployeeIdAsync(
                    employee.Id,
                    cancellationToken);

        if (employmentHistory.EmployeeId !=
            employee.Id)
        {
            return Failure(
                "Lịch sử làm việc không thuộc nhân viên.");
        }

        EmploymentPeriod? employmentPeriod =
            ResolveEmploymentPeriod(
                employmentHistory,
                request.StartDate,
                request.EndDate);

        if (employmentPeriod is null)
        {
            return Failure(
                "Khoảng nghỉ phép không nằm trọn trong một giai đoạn làm việc của nhân viên.");
        }

        try
        {
            LeaveRequestEligibilityPolicy
                .EnsureCanRequest(
                    employee.Id,
                    employmentPeriod,
                    leaveType,
                    request.StartDate,
                    request.EndDate);
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

        IReadOnlyList<LeaveRequest> overlappingRequests =
            await _leaveRequestRepository
                .GetOverlappingByEmployeeAsync(
                    employee.Id,
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);

        try
        {
            LeaveRequestOverlapPolicy
                .EnsureNoOverlap(
                    employee.Id,
                    request.StartDate,
                    request.EndDate,
                    overlappingRequests);
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

        DateTime submittedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        var leaveRequest =
            new LeaveRequest(
                Guid.NewGuid(),
                employee.Id,
                employmentPeriod.Id,
                leaveType.Id,
                request.StartDate,
                request.EndDate,
                request.Reason,
                submittedAtUtc);

        await _persistence.SubmitAsync(
            leaveRequest,
            cancellationToken);

        return new SubmitLeaveRequestResult(
            IsSuccessful: true,
            LeaveRequestId:
                leaveRequest.Id,
            Status:
                leaveRequest.Status);
    }

    private static EmploymentPeriod? ResolveEmploymentPeriod(
        EmploymentHistory employmentHistory,
        DateOnly startDate,
        DateOnly endDate)
    {
        List<EmploymentPeriod> matchingPeriods =
            employmentHistory
                .Periods
                .Where(
                    period =>
                        startDate >=
                            period.StartDate
                        && (
                            !period.EndDate.HasValue
                            || endDate <=
                                period.EndDate.Value
                        ))
                .ToList();

        return matchingPeriods.Count == 1
            ? matchingPeriods[0]
            : null;
    }

    private static SubmitLeaveRequestResult Failure(
        string errorMessage)
    {
        return new SubmitLeaveRequestResult(
            IsSuccessful: false,
            ErrorMessage:
                errorMessage);
    }
}
