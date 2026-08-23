using HrManagement.Application.Attendance.Expectations;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Records;

public sealed class AttendancePunchContextResolver
    : IAttendancePunchContextResolver
{
    private readonly IEmployeeWorkScheduleAssignmentRepository
        _assignmentRepository;

    private readonly IWorkScheduleRepository
        _workScheduleRepository;

    private readonly IWorkExpectationResolver
        _workExpectationResolver;

    private readonly IAttendanceTimeZoneConverter
        _timeZoneConverter;

    public AttendancePunchContextResolver(
        IEmployeeWorkScheduleAssignmentRepository assignmentRepository,
        IWorkScheduleRepository workScheduleRepository,
        IWorkExpectationResolver workExpectationResolver,
        IAttendanceTimeZoneConverter timeZoneConverter)
    {
        _assignmentRepository =
            assignmentRepository;

        _workScheduleRepository =
            workScheduleRepository;

        _workExpectationResolver =
            workExpectationResolver;

        _timeZoneConverter =
            timeZoneConverter;
    }

    public async Task<AttendancePunchContextResolutionResult>
        ResolveAsync(
            Guid employeeId,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (occurredAtUtc == default)
        {
            return Failure(
                "Thời điểm chấm công không hợp lệ.");
        }

        if (occurredAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            return Failure(
                "Thời điểm chấm công phải được lưu theo UTC.");
        }

        IReadOnlyList<EmployeeWorkScheduleAssignment> assignments =
            await _assignmentRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        if (assignments.Count == 0)
        {
            return Failure(
                "Không tìm thấy phân lịch làm việc của nhân viên.");
        }

        var overnightCandidates =
            new List<Candidate>();

        var sameDayCandidates =
            new List<Candidate>();

        bool missingExpectation =
            false;

        try
        {
            foreach (EmployeeWorkScheduleAssignment assignment
                     in assignments)
            {
                WorkSchedule? schedule =
                    await _workScheduleRepository
                        .GetByIdAsync(
                            assignment.WorkScheduleId,
                            cancellationToken);

                if (schedule is null)
                {
                    return Failure(
                        "Không tìm thấy lịch làm việc của phân lịch.");
                }

                DateTime localOccurredAt;

                try
                {
                    localOccurredAt =
                        _timeZoneConverter
                            .ConvertFromUtc(
                                occurredAtUtc,
                                schedule.TimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return Failure(
                        "Không tìm thấy múi giờ của lịch làm việc.");
                }
                catch (InvalidTimeZoneException)
                {
                    return Failure(
                        "Múi giờ của lịch làm việc không hợp lệ.");
                }

                DateOnly localDate =
                    DateOnly.FromDateTime(
                        localOccurredAt);

                TimeOnly localTime =
                    TimeOnly.FromDateTime(
                        localOccurredAt);

                DateOnly previousDate =
                    localDate.AddDays(
                        -1);

                if (Covers(
                        assignment,
                        previousDate))
                {
                    ResolvedWorkExpectation? previousExpectation =
                        await _workExpectationResolver
                            .ResolveAsync(
                                schedule.Id,
                                previousDate,
                                cancellationToken);

                    if (previousExpectation is not null
                        && previousExpectation.IsWorkingDay
                        && previousExpectation.IsOvernight
                        && previousExpectation.EndTime.HasValue
                        && localTime <=
                            previousExpectation.EndTime.Value)
                    {
                        overnightCandidates.Add(
                            new Candidate(
                                assignment,
                                schedule,
                                previousExpectation,
                                previousDate));

                        continue;
                    }
                }

                if (!Covers(
                        assignment,
                        localDate))
                {
                    continue;
                }

                ResolvedWorkExpectation? currentExpectation =
                    await _workExpectationResolver
                        .ResolveAsync(
                            schedule.Id,
                            localDate,
                            cancellationToken);

                if (currentExpectation is null)
                {
                    missingExpectation =
                        true;

                    continue;
                }

                sameDayCandidates.Add(
                    new Candidate(
                        assignment,
                        schedule,
                        currentExpectation,
                        localDate));
            }
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

        if (overnightCandidates.Count > 1)
        {
            return Failure(
                "Không thể xác định duy nhất phân lịch cho ca qua đêm.");
        }

        if (overnightCandidates.Count == 1)
        {
            return Success(
                overnightCandidates[0]);
        }

        if (sameDayCandidates.Count > 1)
        {
            return Failure(
                "Không thể xác định duy nhất phân lịch cho ngày chấm công.");
        }

        if (sameDayCandidates.Count == 1)
        {
            return Success(
                sameDayCandidates[0]);
        }

        if (missingExpectation)
        {
            return Failure(
                "Lịch làm việc chưa có cấu hình kỳ vọng cho ngày chấm công.");
        }

        return Failure(
            "Không tìm thấy phân lịch làm việc phù hợp với thời điểm chấm công.");
    }

    private static bool Covers(
        EmployeeWorkScheduleAssignment assignment,
        DateOnly workDate)
    {
        if (workDate <
            assignment.EffectiveFrom)
        {
            return false;
        }

        return !assignment.EffectiveTo.HasValue
            || workDate <=
                assignment.EffectiveTo.Value;
    }

    private static AttendancePunchContextResolutionResult
        Success(
            Candidate candidate)
    {
        ResolvedWorkExpectation expectation =
            candidate.Expectation;

        return new AttendancePunchContextResolutionResult(
            IsSuccessful: true,
            Context:
                new AttendancePunchContext(
                    candidate.Assignment.EmployeeId,
                    candidate.Assignment.EmploymentPeriodId,
                    candidate.Assignment.Id,
                    candidate.Schedule.Id,
                    candidate.WorkDate,
                    candidate.Schedule.TimeZoneId,
                    expectation.IsWorkingDay,
                    expectation.StartTime,
                    expectation.EndTime,
                    expectation.BreakMinutes,
                    expectation.Source,
                    expectation.SourceId,
                    expectation.SourceName));
    }

    private static AttendancePunchContextResolutionResult
        Failure(
            string errorMessage)
    {
        return new AttendancePunchContextResolutionResult(
            IsSuccessful: false,
            ErrorMessage:
                errorMessage);
    }

    private sealed record Candidate(
        EmployeeWorkScheduleAssignment Assignment,
        WorkSchedule Schedule,
        ResolvedWorkExpectation Expectation,
        DateOnly WorkDate);
}
