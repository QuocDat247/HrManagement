using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;

namespace HrManagement.Application.Attendance.Schedules;

public sealed class EmployeeWorkScheduleAssignmentService
    : IEmployeeWorkScheduleAssignmentService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmploymentHistoryRepository
        _employmentHistoryRepository;

    private readonly IWorkScheduleRepository
        _workScheduleRepository;

    private readonly IEmployeeWorkScheduleAssignmentRepository
        _assignmentRepository;

    private readonly IEmployeeWorkScheduleAssignmentPersistence
        _persistence;

    public EmployeeWorkScheduleAssignmentService(
        IEmployeeRepository employeeRepository,
        IEmploymentHistoryRepository employmentHistoryRepository,
        IWorkScheduleRepository workScheduleRepository,
        IEmployeeWorkScheduleAssignmentRepository assignmentRepository,
        IEmployeeWorkScheduleAssignmentPersistence persistence)
    {
        _employeeRepository =
            employeeRepository;

        _employmentHistoryRepository =
            employmentHistoryRepository;

        _workScheduleRepository =
            workScheduleRepository;

        _assignmentRepository =
            assignmentRepository;

        _persistence =
            persistence;
    }

    public async Task<AssignEmployeeWorkScheduleResult>
        AssignAsync(
            AssignEmployeeWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (request.WorkScheduleId == Guid.Empty)
        {
            return Failure(
                "Vui lòng chọn lịch làm việc.");
        }

        if (request.EffectiveFrom == default)
        {
            return Failure(
                "Ngày bắt đầu áp dụng lịch làm việc không hợp lệ.");
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

        if (employee.Status is not EmployeeStatus.Active
            and not EmployeeStatus.OnLeave)
        {
            return Failure(
                "Chỉ có thể phân lịch cho nhân viên đang làm việc hoặc nghỉ phép.");
        }

        WorkSchedule? schedule =
            await _workScheduleRepository
                .GetByIdAsync(
                    request.WorkScheduleId,
                    cancellationToken);

        if (schedule is null)
        {
            return Failure(
                "Không tìm thấy lịch làm việc.");
        }

        if (!schedule.IsActive)
        {
            return Failure(
                "Lịch làm việc đã ngừng sử dụng.");
        }

        EmploymentHistory employmentHistory =
            await _employmentHistoryRepository
                .GetByEmployeeIdAsync(
                    employee.Id,
                    cancellationToken);

        EmploymentPeriod? currentPeriod =
            employmentHistory.CurrentPeriod;

        if (currentPeriod is null)
        {
            return Failure(
                "Không tìm thấy giai đoạn làm việc đang mở của nhân viên.");
        }

        IReadOnlyList<EmployeeWorkScheduleAssignment> assignments =
            await _assignmentRepository
                .GetByEmployeeIdAsync(
                    employee.Id,
                    cancellationToken);

        List<EmployeeWorkScheduleAssignment> periodAssignments =
            assignments
                .Where(
                    assignment =>
                        assignment.EmploymentPeriodId ==
                        currentPeriod.Id)
                .OrderBy(
                    assignment =>
                        assignment.EffectiveFrom)
                .ThenBy(
                    assignment =>
                        assignment.Id)
                .ToList();

        if (periodAssignments.Any(
                assignment =>
                    assignment.EmployeeId !=
                    employee.Id))
        {
            return Failure(
                "Lịch sử phân lịch làm việc chứa dữ liệu không thuộc nhân viên.");
        }

        List<EmployeeWorkScheduleAssignment> openAssignments =
            periodAssignments
                .Where(
                    assignment =>
                        assignment.IsOpen)
                .ToList();

        if (openAssignments.Count > 1)
        {
            return Failure(
                "Lịch sử phân lịch làm việc có nhiều hơn một phân lịch đang mở.");
        }

        if (periodAssignments.Count == 0)
        {
            return await CreateInitialAssignmentAsync(
                employee,
                currentPeriod,
                schedule,
                request.EffectiveFrom,
                cancellationToken);
        }

        if (openAssignments.Count == 0)
        {
            return Failure(
                "Lịch sử phân lịch làm việc không có phân lịch đang mở.");
        }

        EmployeeWorkScheduleAssignment tailAssignment =
            openAssignments[0];

        if (tailAssignment.WorkScheduleId ==
            schedule.Id)
        {
            return Failure(
                "Lịch làm việc mới phải khác lịch đang được phân.");
        }

        DateOnly previousEffectiveTo;

        try
        {
            previousEffectiveTo =
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .CalculatePreviousEffectiveTo(
                        tailAssignment,
                        request.EffectiveFrom);
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

        var closedAssignment =
            new EmployeeWorkScheduleAssignment(
                tailAssignment.Id,
                tailAssignment.EmployeeId,
                tailAssignment.EmploymentPeriodId,
                tailAssignment.WorkScheduleId,
                tailAssignment.EffectiveFrom,
                previousEffectiveTo);

        var newAssignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employee.Id,
                currentPeriod.Id,
                schedule.Id,
                request.EffectiveFrom);

        try
        {
            EmployeeWorkScheduleAssignmentTimelinePolicy
                .EnsureWithinEmploymentPeriod(
                    closedAssignment,
                    currentPeriod);

            EmployeeWorkScheduleAssignmentTimelinePolicy
                .EnsureWithinEmploymentPeriod(
                    newAssignment,
                    currentPeriod);

            List<EmployeeWorkScheduleAssignment>
                validationTimeline =
                    periodAssignments
                        .Where(
                            assignment =>
                                assignment.Id !=
                                tailAssignment.Id)
                        .Append(
                            closedAssignment)
                        .ToList();

            EmployeeWorkScheduleAssignmentTimelinePolicy
                .EnsureNoOverlap(
                    newAssignment,
                    validationTimeline);
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
            closedAssignment,
            newAssignment,
            cancellationToken);

        return Success();
    }

    private async Task<AssignEmployeeWorkScheduleResult>
        CreateInitialAssignmentAsync(
            Employee employee,
            EmploymentPeriod currentPeriod,
            WorkSchedule schedule,
            DateOnly effectiveFrom,
            CancellationToken cancellationToken)
    {
        var newAssignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employee.Id,
                currentPeriod.Id,
                schedule.Id,
                effectiveFrom);

        try
        {
            EmployeeWorkScheduleAssignmentTimelinePolicy
                .EnsureWithinEmploymentPeriod(
                    newAssignment,
                    currentPeriod);
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
            closedAssignment: null,
            newAssignment,
            cancellationToken);

        return Success();
    }

    private static AssignEmployeeWorkScheduleResult
        Success()
    {
        return new AssignEmployeeWorkScheduleResult(
            IsSuccessful: true);
    }

    private static AssignEmployeeWorkScheduleResult
        Failure(
            string errorMessage)
    {
        return new AssignEmployeeWorkScheduleResult(
            IsSuccessful: false,
            ErrorMessage: errorMessage);
    }
}
