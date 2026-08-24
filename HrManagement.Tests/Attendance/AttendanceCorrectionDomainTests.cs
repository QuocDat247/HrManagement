using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceCorrectionDomainTests
{
    [Fact]
    public void AddEvent_WithValidState_CreatesCorrection()
    {
        AttendanceCorrection correction =
            CreateAdd();

        Assert.Equal(
            AttendanceCorrectionKind.AddEvent,
            correction.Kind);

        Assert.False(
            correction.HasBeforeState);

        Assert.True(
            correction.HasAfterState);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            correction.AfterEventType);

        Assert.Equal(
            Utc(8),
            correction.AfterOccurredAtUtc);

        Assert.Equal(
            "Bổ sung chấm vào",
            correction.Reason);
    }

    [Fact]
    public void ChangeEvent_WithValidStates_CreatesCorrection()
    {
        AttendanceCorrection correction =
            CreateChange();

        Assert.True(
            correction.HasBeforeState);

        Assert.True(
            correction.HasAfterState);

        Assert.Equal(
            Utc(8),
            correction.BeforeOccurredAtUtc);

        Assert.Equal(
            Utc(8, 15),
            correction.AfterOccurredAtUtc);
    }

    [Fact]
    public void VoidEvent_WithValidBeforeState_CreatesCorrection()
    {
        AttendanceCorrection correction =
            CreateVoid();

        Assert.True(
            correction.HasBeforeState);

        Assert.False(
            correction.HasAfterState);

        Assert.Null(
            correction.AfterEventType);

        Assert.Null(
            correction.AfterOccurredAtUtc);
    }

    [Fact]
    public void AddEvent_WithBeforeState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.AddEvent,
                    AttendanceEventType.ClockIn,
                    Utc(7, 50),
                    AttendanceEventType.ClockIn,
                    Utc(8)));
    }

    [Fact]
    public void AddEvent_WithoutAfterState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.AddEvent,
                    null,
                    null,
                    null,
                    null));
    }

    [Fact]
    public void ChangeEvent_WithoutBeforeState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.ChangeEvent,
                    null,
                    null,
                    AttendanceEventType.ClockIn,
                    Utc(8)));
    }

    [Fact]
    public void ChangeEvent_WithoutAfterState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.ChangeEvent,
                    AttendanceEventType.ClockIn,
                    Utc(8),
                    null,
                    null));
    }

    [Fact]
    public void ChangeEvent_WithIdenticalStates_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.ChangeEvent,
                    AttendanceEventType.ClockIn,
                    Utc(8),
                    AttendanceEventType.ClockIn,
                    Utc(8)));
    }

    [Fact]
    public void VoidEvent_WithoutBeforeState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.VoidEvent,
                    null,
                    null,
                    null,
                    null));
    }

    [Fact]
    public void VoidEvent_WithAfterState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.VoidEvent,
                    AttendanceEventType.ClockOut,
                    Utc(17),
                    AttendanceEventType.ClockOut,
                    Utc(17, 5)));
    }

    [Fact]
    public void Constructor_WithInvalidRevision_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AttendanceCorrection(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    0,
                    AttendanceCorrectionKind.AddEvent,
                    null,
                    null,
                    AttendanceEventType.ClockIn,
                    Utc(8),
                    "Bổ sung",
                    Utc(18),
                    "user-1",
                    "admin"));
    }

    [Fact]
    public void Constructor_NormalizesReasonAndActor()
    {
        AttendanceCorrection correction =
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                AttendanceCorrectionKind.AddEvent,
                null,
                null,
                AttendanceEventType.ClockIn,
                Utc(8),
                "  Bổ sung chấm vào  ",
                Utc(18),
                "  user-1  ",
                "  admin  ");

        Assert.Equal(
            "Bổ sung chấm vào",
            correction.Reason);

        Assert.Equal(
            "user-1",
            correction.ActorUserId);

        Assert.Equal(
            "admin",
            correction.ActorUsername);
    }

    [Fact]
    public void Constructor_WithPartialEventState_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Create(
                    AttendanceCorrectionKind.AddEvent,
                    null,
                    null,
                    AttendanceEventType.ClockIn,
                    null));
    }

    private static AttendanceCorrection CreateAdd()
    {
        return Create(
            AttendanceCorrectionKind.AddEvent,
            null,
            null,
            AttendanceEventType.ClockIn,
            Utc(8));
    }

    private static AttendanceCorrection CreateChange()
    {
        return Create(
            AttendanceCorrectionKind.ChangeEvent,
            AttendanceEventType.ClockIn,
            Utc(8),
            AttendanceEventType.ClockIn,
            Utc(8, 15));
    }

    private static AttendanceCorrection CreateVoid()
    {
        return Create(
            AttendanceCorrectionKind.VoidEvent,
            AttendanceEventType.ClockOut,
            Utc(17),
            null,
            null);
    }

    private static AttendanceCorrection Create(
        AttendanceCorrectionKind kind,
        AttendanceEventType? beforeEventType,
        DateTime? beforeOccurredAtUtc,
        AttendanceEventType? afterEventType,
        DateTime? afterOccurredAtUtc)
    {
        return new AttendanceCorrection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            kind,
            beforeEventType,
            beforeOccurredAtUtc,
            afterEventType,
            afterOccurredAtUtc,
            kind switch
            {
                AttendanceCorrectionKind.AddEvent =>
                    "Bổ sung chấm vào",

                AttendanceCorrectionKind.ChangeEvent =>
                    "Sửa giờ chấm công",

                AttendanceCorrectionKind.VoidEvent =>
                    "Hủy sự kiện nhập sai",

                _ =>
                    "Điều chỉnh"
            },
            Utc(18),
            "user-1",
            "admin");
    }

    private static DateTime Utc(
        int hour,
        int minute = 0)
    {
        return new DateTime(
            2026,
            8,
            24,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}
