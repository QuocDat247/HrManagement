using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Generation;

public interface IDailyAttendanceGenerationPersistence
{
    Task<IReadOnlyList<DailyAttendanceGenerationCandidate>>
        GetCandidatesAsync(
            DateOnly workDate,
            Guid? employeeId = null,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>>
        GetExistingEmployeeIdsAsync(
            DateOnly workDate,
            IReadOnlyCollection<Guid> employeeIds,
            CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyList<AttendanceRecord> records,
        CancellationToken cancellationToken = default);
}
