using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Records;

public sealed class EfAttendanceEventRepository
    : IAttendanceEventRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAttendanceEventRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<AttendanceEvent>>
        GetByAttendanceRecordIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
    {
        if (attendanceRecordId == Guid.Empty)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .AttendanceEvents
            .AsNoTracking()
            .Where(
                attendanceEvent =>
                    attendanceEvent.AttendanceRecordId ==
                    attendanceRecordId)
            .OrderBy(
                attendanceEvent =>
                    attendanceEvent.OccurredAtUtc)
            .ThenBy(
                attendanceEvent =>
                    attendanceEvent.Id)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<AttendanceEvent?> GetLatestByEmployeeIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .AttendanceEvents
            .AsNoTracking()
            .Where(
                attendanceEvent =>
                    attendanceEvent.EmployeeId ==
                    employeeId)
            .OrderByDescending(
                attendanceEvent =>
                    attendanceEvent.OccurredAtUtc)
            .ThenByDescending(
                attendanceEvent =>
                    attendanceEvent.Id)
            .FirstOrDefaultAsync(
                cancellationToken);
    }
}
