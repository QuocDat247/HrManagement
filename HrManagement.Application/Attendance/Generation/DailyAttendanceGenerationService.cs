using HrManagement.Application.Attendance.Expectations;
using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Generation;

public sealed class DailyAttendanceGenerationService
    : IDailyAttendanceGenerationService
{
    private readonly IDailyAttendanceGenerationPersistence
        _persistence;

    private readonly IWorkExpectationResolver
        _expectationResolver;

    private readonly IAttendancePeriodLockPolicy
        _periodLockPolicy;

    public DailyAttendanceGenerationService(
        IDailyAttendanceGenerationPersistence persistence,
        IWorkExpectationResolver expectationResolver,
        IAttendancePeriodLockPolicy periodLockPolicy)
    {
        _persistence =
            persistence;

        _expectationResolver =
            expectationResolver;

        _periodLockPolicy =
            periodLockPolicy;
    }

    public async Task<GenerateDailyAttendanceResult> GenerateAsync(
        GenerateDailyAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.WorkDate == default)
        {
            return Failure(
                "Ngày sinh dữ liệu chấm công không hợp lệ.");
        }

        if (request.EmployeeId.HasValue
            && request.EmployeeId.Value ==
                Guid.Empty)
        {
            return Failure(
                "Nhân viên không hợp lệ.");
        }

        bool isPeriodLocked =
            await _periodLockPolicy
                .IsLockedAsync(
                    request.WorkDate,
                    cancellationToken);

        if (isPeriodLocked)
        {
            return Failure(
                "Kỳ công của ngày đã chọn đã được đóng. Không thể sinh dữ liệu chấm công.");
        }

        IReadOnlyList<DailyAttendanceGenerationCandidate>
            candidates =
                await _persistence
                    .GetCandidatesAsync(
                        request.WorkDate,
                        request.EmployeeId,
                        cancellationToken);

        if (request.EmployeeId.HasValue
            && candidates.Count == 0)
        {
            return Failure(
                "Không tìm thấy phân lịch làm việc phù hợp cho nhân viên tại ngày đã chọn.");
        }

        Guid? duplicatedEmployeeId =
            candidates
                .GroupBy(
                    candidate =>
                        candidate.EmployeeId)
                .Where(
                    group =>
                        group.Count() > 1)
                .Select(
                    group =>
                        (Guid?)group.Key)
                .FirstOrDefault();

        if (duplicatedEmployeeId.HasValue)
        {
            return Failure(
                "Phát hiện nhiều phân lịch làm việc cùng hiệu lực cho một nhân viên.");
        }

        Guid[] employeeIds =
            candidates
                .Select(
                    candidate =>
                        candidate.EmployeeId)
                .Distinct()
                .ToArray();

        IReadOnlyList<Guid> existingEmployeeIds =
            employeeIds.Length == 0
                ? Array.Empty<Guid>()
                : await _persistence
                    .GetExistingEmployeeIdsAsync(
                        request.WorkDate,
                        employeeIds,
                        cancellationToken);

        HashSet<Guid> existingSet =
            existingEmployeeIds
                .ToHashSet();

        DailyAttendanceGenerationCandidate[] pendingCandidates =
            candidates
                .Where(
                    candidate =>
                        !existingSet.Contains(
                            candidate.EmployeeId))
                .ToArray();

        if (pendingCandidates.Length == 0)
        {
            return new GenerateDailyAttendanceResult(
                true,
                CreatedCount:
                    0,
                SkippedExistingCount:
                    candidates.Count);
        }

        Guid[] workScheduleIds =
            pendingCandidates
                .Select(
                    candidate =>
                        candidate.WorkScheduleId)
                .Distinct()
                .ToArray();

        IReadOnlyDictionary<Guid, ResolvedWorkExpectation>
            expectations;

        try
        {
            expectations =
                await _expectationResolver
                    .ResolveManyAsync(
                        workScheduleIds,
                        request.WorkDate,
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

        Guid? unresolvedScheduleId =
            workScheduleIds
                .Where(
                    workScheduleId =>
                        !expectations.ContainsKey(
                            workScheduleId))
                .Select(
                    workScheduleId =>
                        (Guid?)workScheduleId)
                .FirstOrDefault();

        if (unresolvedScheduleId.HasValue)
        {
            return Failure(
                "Lịch làm việc chưa có cấu hình kỳ vọng cho ngày sinh dữ liệu chấm công.");
        }

        var records =
            new List<AttendanceRecord>(
                pendingCandidates.Length);

        try
        {
            foreach (DailyAttendanceGenerationCandidate candidate
                     in pendingCandidates)
            {
                ResolvedWorkExpectation expectation =
                    expectations[
                        candidate.WorkScheduleId];

                records.Add(
                    new AttendanceRecord(
                        Guid.NewGuid(),
                        candidate.EmployeeId,
                        candidate.EmploymentPeriodId,
                        candidate.WorkScheduleAssignmentId,
                        candidate.WorkScheduleId,
                        request.WorkDate,
                        candidate.TimeZoneId,
                        expectation.IsWorkingDay,
                        expectation.StartTime,
                        expectation.EndTime,
                        expectation.BreakMinutes,
                        expectation.Source,
                        expectation.SourceId,
                        expectation.SourceName));
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        await _persistence
            .AddRangeAsync(
                records,
                cancellationToken);

        return new GenerateDailyAttendanceResult(
            true,
            CreatedCount:
                records.Count,
            SkippedExistingCount:
                candidates.Count
                - records.Count);
    }

    private static GenerateDailyAttendanceResult Failure(
        string errorMessage)
    {
        return new GenerateDailyAttendanceResult(
            false,
            ErrorMessage:
                errorMessage);
    }
}
