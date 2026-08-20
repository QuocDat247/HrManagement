using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePunchSequencePolicyTests
{
    [Fact]
    public void EmptyTimeline_AllowsClockIn()
    {
        AttendancePunchSequencePolicy
            .EnsureCanAppend(
                [],
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));
    }

    [Fact]
    public void EmptyTimeline_RejectsClockOut()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                AttendancePunchSequencePolicy
                    .EnsureCanAppend(
                        [],
                        AttendanceEventType.ClockOut,
                        Utc(
                            8,
                            0)));
    }

    [Fact]
    public void AfterClockIn_AllowsClockOut()
    {
        PunchIds ids =
            CreateIds();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0))
        ];

        AttendancePunchSequencePolicy
            .EnsureCanAppend(
                events,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0));
    }

    [Fact]
    public void CompletedPair_AllowsAnotherClockIn()
    {
        PunchIds ids =
            CreateIds();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                ids,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0))
        ];

        AttendancePunchSequencePolicy
            .EnsureCanAppend(
                events,
                AttendanceEventType.ClockIn,
                Utc(
                    13,
                    0));
    }

    [Fact]
    public void RepeatedClockIn_IsRejected()
    {
        PunchIds ids =
            CreateIds();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0))
        ];

        Assert.Throws<InvalidOperationException>(
            () =>
                AttendancePunchSequencePolicy
                    .EnsureCanAppend(
                        events,
                        AttendanceEventType.ClockIn,
                        Utc(
                            9,
                            0)));
    }

    [Fact]
    public void NewTimestampNotAfterLastEvent_IsRejected()
    {
        PunchIds ids =
            CreateIds();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0))
        ];

        Assert.Throws<InvalidOperationException>(
            () =>
                AttendancePunchSequencePolicy
                    .EnsureCanAppend(
                        events,
                        AttendanceEventType.ClockOut,
                        Utc(
                            8,
                            0)));
    }

    [Fact]
    public void ExistingTimelineWithMixedRecord_IsRejected()
    {
        PunchIds ids =
            CreateIds();

        AttendanceEvent first =
            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        AttendanceEvent second =
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ids.EmployeeId,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0));

        IReadOnlyList<AttendanceEvent> events =
        [
            first,
            second
        ];

        Assert.Throws<InvalidOperationException>(
            () =>
                AttendancePunchSequencePolicy
                    .EnsureCanAppend(
                        events,
                        AttendanceEventType.ClockIn,
                        Utc(
                            13,
                            0)));
    }

    [Fact]
    public void ExistingTimelineWithBrokenSequence_IsRejected()
    {
        PunchIds ids =
            CreateIds();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                ids,
                AttendanceEventType.ClockIn,
                Utc(
                    9,
                    0))
        ];

        Assert.Throws<InvalidOperationException>(
            () =>
                AttendancePunchSequencePolicy
                    .EnsureCanAppend(
                        events,
                        AttendanceEventType.ClockOut,
                        Utc(
                            10,
                            0)));
    }

    private static AttendanceEvent CreateEvent(
        PunchIds ids,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            Guid.NewGuid(),
            ids.AttendanceRecordId,
            ids.EmployeeId,
            eventType,
            occurredAtUtc);
    }

    private static PunchIds CreateIds()
    {
        return new PunchIds(
            Guid.NewGuid(),
            Guid.NewGuid());
    }

    private static DateTime Utc(
        int hour,
        int minute)
    {
        return new DateTime(
            2026,
            8,
            20,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }

    private sealed record PunchIds(
        Guid AttendanceRecordId,
        Guid EmployeeId);
}
