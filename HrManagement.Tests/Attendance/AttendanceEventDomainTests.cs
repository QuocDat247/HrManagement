using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceEventDomainTests
{
    [Fact]
    public void Constructor_WithUtcTimestamp_CreatesEvent()
    {
        Guid id =
            Guid.NewGuid();

        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        DateTime occurredAtUtc =
            new(
                2026,
                8,
                20,
                1,
                30,
                15,
                DateTimeKind.Utc);

        var attendanceEvent =
            new AttendanceEvent(
                id,
                recordId,
                employeeId,
                AttendanceEventType.ClockIn,
                occurredAtUtc);

        Assert.Equal(
            id,
            attendanceEvent.Id);

        Assert.Equal(
            recordId,
            attendanceEvent.AttendanceRecordId);

        Assert.Equal(
            employeeId,
            attendanceEvent.EmployeeId);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            attendanceEvent.EventType);

        Assert.Equal(
            occurredAtUtc,
            attendanceEvent.OccurredAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            attendanceEvent.OccurredAtUtc.Kind);
    }

    [Fact]
    public void Constructor_WhenIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceEvent(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    AttendanceEventType.ClockIn,
                    UtcNow()));
    }

    [Fact]
    public void Constructor_WhenAttendanceRecordIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceEvent(
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    AttendanceEventType.ClockIn,
                    UtcNow()));
    }

    [Fact]
    public void Constructor_WhenEmployeeIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    AttendanceEventType.ClockIn,
                    UtcNow()));
    }

    [Fact]
    public void Constructor_WhenEventTypeIsUndefined_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AttendanceEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    (AttendanceEventType)99,
                    UtcNow()));
    }

    [Fact]
    public void Constructor_WhenTimestampIsLocal_Throws()
    {
        DateTime localTimestamp =
            new(
                2026,
                8,
                20,
                8,
                0,
                0,
                DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    AttendanceEventType.ClockOut,
                    localTimestamp));
    }

    [Fact]
    public void Constructor_WhenTimestampKindIsUnspecified_Throws()
    {
        DateTime timestamp =
            new(
                2026,
                8,
                20,
                8,
                0,
                0,
                DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    AttendanceEventType.ClockIn,
                    timestamp));
    }

    private static DateTime UtcNow()
    {
        return new DateTime(
            2026,
            8,
            20,
            1,
            0,
            0,
            DateTimeKind.Utc);
    }
}
