using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Infrastructure.Attendance.Calculations;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceScheduleWindowResolverTests
{
    [Fact]
    public void DayShiftInUtc_ResolvesSameCalendarDay()
    {
        AttendanceRecord record =
            CreateWorkingRecord(
                "UTC",
                new DateOnly(
                    2026,
                    8,
                    20),
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0));

        var resolver =
            new SystemAttendanceScheduleWindowResolver();

        AttendanceScheduleWindow? result =
            resolver.Resolve(
                record);

        Assert.NotNull(
            result);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                8,
                0),
            result!.ExpectedStartAtUtc);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                17,
                0),
            result.ExpectedEndAtUtc);
    }

    [Fact]
    public void OvernightShift_EndFallsOnFollowingDay()
    {
        AttendanceRecord record =
            CreateWorkingRecord(
                "UTC",
                new DateOnly(
                    2026,
                    8,
                    20),
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0));

        var resolver =
            new SystemAttendanceScheduleWindowResolver();

        AttendanceScheduleWindow? result =
            resolver.Resolve(
                record);

        Assert.NotNull(
            result);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                22,
                0),
            result!.ExpectedStartAtUtc);

        Assert.Equal(
            Utc(
                2026,
                8,
                21,
                6,
                0),
            result.ExpectedEndAtUtc);
    }

    [Fact]
    public void SouthEastAsiaSchedule_ConvertsLocalTimesToUtc()
    {
        AttendanceRecord record =
            CreateWorkingRecord(
                "SE Asia Standard Time",
                new DateOnly(
                    2026,
                    8,
                    20),
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0));

        var resolver =
            new SystemAttendanceScheduleWindowResolver();

        AttendanceScheduleWindow? result =
            resolver.Resolve(
                record);

        Assert.NotNull(
            result);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                1,
                0),
            result!.ExpectedStartAtUtc);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                10,
                0),
            result.ExpectedEndAtUtc);
    }

    [Fact]
    public void OvernightSouthEastAsiaSchedule_ConvertsFollowingLocalDay()
    {
        AttendanceRecord record =
            CreateWorkingRecord(
                "SE Asia Standard Time",
                new DateOnly(
                    2026,
                    8,
                    20),
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0));

        var resolver =
            new SystemAttendanceScheduleWindowResolver();

        AttendanceScheduleWindow? result =
            resolver.Resolve(
                record);

        Assert.NotNull(
            result);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                15,
                0),
            result!.ExpectedStartAtUtc);

        Assert.Equal(
            Utc(
                2026,
                8,
                20,
                23,
                0),
            result.ExpectedEndAtUtc);
    }

    [Fact]
    public void NonWorkingDay_ReturnsNoScheduleWindow()
    {
        AttendanceRecord record =
            CreateNonWorkingRecord();

        var resolver =
            new SystemAttendanceScheduleWindowResolver();

        AttendanceScheduleWindow? result =
            resolver.Resolve(
                record);

        Assert.Null(
            result);
    }

    [Fact]
    public void InvalidTimeZone_IsRejected()
    {
        AttendanceRecord record =
            CreateWorkingRecord(
                "NOT-A-REAL-TIME-ZONE",
                new DateOnly(
                    2026,
                    8,
                    20),
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0));

        var resolver =
            new SystemAttendanceScheduleWindowResolver();

        Assert.Throws<TimeZoneNotFoundException>(
            () =>
                resolver.Resolve(
                    record));
    }

    private static AttendanceRecord CreateWorkingRecord(
        string timeZoneId,
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            workDate,
            timeZoneId,
            true,
            startTime,
            endTime,
            60);
    }

    private static AttendanceRecord CreateNonWorkingRecord()
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                22),
            "SE Asia Standard Time",
            false);
    }

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}
