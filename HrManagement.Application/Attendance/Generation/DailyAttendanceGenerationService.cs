using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Generation;

public sealed class DailyAttendanceGenerationService
    : IDailyAttendanceGenerationService
{
    private readonly IDailyAttendanceGenerationPersistence
        _persistence;

    public DailyAttendanceGenerationService(
        IDailyAttendanceGenerationPersistence persistence)
    {
        _persistence =
            persistence;
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
            && request.EmployeeId.Value == Guid.Empty)
        {
            return Failure(
                "Nhân viên không hợp lệ.");
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

        var existingSet =
            existingEmployeeIds.ToHashSet();

        var records =
            new List<AttendanceRecord>();

        try
        {
            foreach (DailyAttendanceGenerationCandidate candidate
                     in candidates)
            {
                if (existingSet.Contains(
                        candidate.EmployeeId))
                {
                    continue;
                }

                records.Add(
                    new AttendanceRecord(
                        Guid.NewGuid(),
                        candidate.EmployeeId,
                        candidate.EmploymentPeriodId,
                        candidate.WorkScheduleAssignmentId,
                        candidate.WorkScheduleId,
                        request.WorkDate,
                        candidate.TimeZoneId,
                        candidate.IsWorkingDay,
                        candidate.ExpectedStartTime,
                        candidate.ExpectedEndTime,
                        candidate.ExpectedBreakMinutes));
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        if (records.Count > 0)
        {
            await _persistence
                .AddRangeAsync(
                    records,
                    cancellationToken);
        }

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
