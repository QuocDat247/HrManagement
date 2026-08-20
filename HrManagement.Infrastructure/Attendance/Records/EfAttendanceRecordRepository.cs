using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Records;

public sealed class EfAttendanceRecordRepository
    : IAttendanceRecordRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAttendanceRecordRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<AttendanceRecord?> GetByIdAsync(
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default)
    {
        if (attendanceRecordId == Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .AttendanceRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                record =>
                    record.Id ==
                    attendanceRecordId,
                cancellationToken);
    }

    public async Task<AttendanceRecord?>
        GetByEmployeeAndWorkDateAsync(
            Guid employeeId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty
            || workDate == default)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .AttendanceRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                record =>
                    record.EmployeeId ==
                        employeeId
                    && record.WorkDate ==
                        workDate,
                cancellationToken);
    }
}
