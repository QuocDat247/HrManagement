using HrManagement.Application.Attendance.Corrections;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class EffectiveAttendanceTimelineResolverTests
{
    private readonly EffectiveAttendanceTimelineResolver
        _resolver =
            new();

    [Fact]
    public void Resolve_RawEventsOnly_ReturnsChronologicalUnmodifiedTimeline()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        AttendanceEvent clockOut =
            RawEvent(
                recordId,
                employeeId,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        AttendanceEvent clockIn =
            RawEvent(
                recordId,
                employeeId,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        IReadOnlyList<EffectiveAttendanceEvent> result =
            _resolver.Resolve(
                recordId,
                employeeId,
                [
                    clockOut,
                    clockIn
                ],
                []);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            clockIn.Id,
            result[0].EventId);

        Assert.Equal(
            clockOut.Id,
            result[1].EventId);

        Assert.All(
            result,
            item =>
            {
                Assert.False(
                    item.IsManual);

                Assert.False(
                    item.IsCorrected);

                Assert.Null(
                    item.LastCorrectionRevision);
            });
    }

    [Fact]
    public void Resolve_AddEvent_AddsManualCorrectedEvent()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceCorrection correction =
            AddCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        EffectiveAttendanceEvent result =
            Assert.Single(
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [],
                    [correction]));

        Assert.Equal(
            eventId,
            result.EventId);

        Assert.True(
            result.IsManual);

        Assert.True(
            result.IsCorrected);

        Assert.Equal(
            1,
            result.LastCorrectionRevision);

        Assert.Equal(
            Utc(
                8),
            result.OccurredAtUtc);
    }

    [Fact]
    public void Resolve_ChangeEvent_UpdatesRawEventAndPreservesLogicalIdentity()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceEvent raw =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection correction =
            ChangeCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    15));

        EffectiveAttendanceEvent result =
            Assert.Single(
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [raw],
                    [correction]));

        Assert.Equal(
            eventId,
            result.EventId);

        Assert.False(
            result.IsManual);

        Assert.True(
            result.IsCorrected);

        Assert.Equal(
            1,
            result.LastCorrectionRevision);

        Assert.Equal(
            Utc(
                8,
                15),
            result.OccurredAtUtc);
    }

    [Fact]
    public void Resolve_VoidEvent_RemovesRawEvent()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceEvent raw =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        AttendanceCorrection correction =
            VoidCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        IReadOnlyList<EffectiveAttendanceEvent> result =
            _resolver.Resolve(
                recordId,
                employeeId,
                [raw],
                [correction]);

        Assert.Empty(
            result);
    }

    [Fact]
    public void Resolve_AddThenChange_PreservesManualOriginAndLatestRevision()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceCorrection add =
            AddCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection change =
            ChangeCorrection(
                recordId,
                employeeId,
                eventId,
                2,
                AttendanceEventType.ClockIn,
                Utc(
                    8),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10));

        EffectiveAttendanceEvent result =
            Assert.Single(
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [],
                    [
                        add,
                        change
                    ]));

        Assert.True(
            result.IsManual);

        Assert.True(
            result.IsCorrected);

        Assert.Equal(
            2,
            result.LastCorrectionRevision);

        Assert.Equal(
            Utc(
                8,
                10),
            result.OccurredAtUtc);
    }

    [Fact]
    public void Resolve_AddThenVoid_RemovesManualEvent()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceCorrection add =
            AddCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection remove =
            VoidCorrection(
                recordId,
                employeeId,
                eventId,
                2,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        IReadOnlyList<EffectiveAttendanceEvent> result =
            _resolver.Resolve(
                recordId,
                employeeId,
                [],
                [
                    add,
                    remove
                ]);

        Assert.Empty(
            result);
    }

    [Fact]
    public void Resolve_CorrectionsOutOfInputOrder_AppliesRevisionOrder()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceEvent raw =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection revision1 =
            ChangeCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10));

        AttendanceCorrection revision2 =
            ChangeCorrection(
                recordId,
                employeeId,
                eventId,
                2,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    20));

        EffectiveAttendanceEvent result =
            Assert.Single(
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [raw],
                    [
                        revision2,
                        revision1
                    ]));

        Assert.Equal(
            Utc(
                8,
                20),
            result.OccurredAtUtc);

        Assert.Equal(
            2,
            result.LastCorrectionRevision);
    }

    [Fact]
    public void Resolve_WithDuplicateRevision_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        AttendanceCorrection first =
            AddCorrection(
                recordId,
                employeeId,
                Guid.NewGuid(),
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection second =
            AddCorrection(
                recordId,
                employeeId,
                Guid.NewGuid(),
                1,
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [],
                    [
                        first,
                        second
                    ]));
    }

    [Fact]
    public void Resolve_WithRevisionGap_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        AttendanceCorrection revision1 =
            AddCorrection(
                recordId,
                employeeId,
                Guid.NewGuid(),
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection revision3 =
            AddCorrection(
                recordId,
                employeeId,
                Guid.NewGuid(),
                3,
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [],
                    [
                        revision1,
                        revision3
                    ]));
    }

    [Fact]
    public void Resolve_ChangeWithStaleBeforeState_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceEvent raw =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection staleCorrection =
            ChangeCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    7,
                    55),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    15));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [raw],
                    [staleCorrection]));
    }

    [Fact]
    public void Resolve_VoidMissingTarget_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        AttendanceCorrection correction =
            VoidCorrection(
                recordId,
                employeeId,
                Guid.NewGuid(),
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [],
                    [correction]));
    }

    [Fact]
    public void Resolve_AddUsingExistingRawEventId_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceEvent raw =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceCorrection correction =
            AddCorrection(
                recordId,
                employeeId,
                eventId,
                1,
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [raw],
                    [correction]));
    }

    [Fact]
    public void Resolve_WithForeignRecordOrEmployee_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        AttendanceEvent foreignRecordEvent =
            RawEvent(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [foreignRecordEvent],
                    []));

        AttendanceCorrection foreignEmployeeCorrection =
            AddCorrection(
                recordId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [],
                    [foreignEmployeeCorrection]));
    }

    [Fact]
    public void Resolve_WithDuplicateRawEventId_Throws()
    {
        Guid recordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceEvent first =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockIn,
                Utc(
                    8));

        AttendanceEvent duplicate =
            RawEvent(
                recordId,
                employeeId,
                eventId,
                AttendanceEventType.ClockOut,
                Utc(
                    17));

        Assert.Throws<InvalidOperationException>(
            () =>
                _resolver.Resolve(
                    recordId,
                    employeeId,
                    [
                        first,
                        duplicate
                    ],
                    []));
    }

    private static AttendanceEvent RawEvent(
        Guid recordId,
        Guid employeeId,
        Guid eventId,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            eventId,
            recordId,
            employeeId,
            eventType,
            occurredAtUtc);
    }

    private static AttendanceCorrection AddCorrection(
        Guid recordId,
        Guid employeeId,
        Guid eventId,
        int revision,
        AttendanceEventType afterEventType,
        DateTime afterOccurredAtUtc)
    {
        return Correction(
            recordId,
            employeeId,
            eventId,
            revision,
            AttendanceCorrectionKind.AddEvent,
            null,
            null,
            afterEventType,
            afterOccurredAtUtc);
    }

    private static AttendanceCorrection ChangeCorrection(
        Guid recordId,
        Guid employeeId,
        Guid eventId,
        int revision,
        AttendanceEventType beforeEventType,
        DateTime beforeOccurredAtUtc,
        AttendanceEventType afterEventType,
        DateTime afterOccurredAtUtc)
    {
        return Correction(
            recordId,
            employeeId,
            eventId,
            revision,
            AttendanceCorrectionKind.ChangeEvent,
            beforeEventType,
            beforeOccurredAtUtc,
            afterEventType,
            afterOccurredAtUtc);
    }

    private static AttendanceCorrection VoidCorrection(
        Guid recordId,
        Guid employeeId,
        Guid eventId,
        int revision,
        AttendanceEventType beforeEventType,
        DateTime beforeOccurredAtUtc)
    {
        return Correction(
            recordId,
            employeeId,
            eventId,
            revision,
            AttendanceCorrectionKind.VoidEvent,
            beforeEventType,
            beforeOccurredAtUtc,
            null,
            null);
    }

    private static AttendanceCorrection Correction(
        Guid recordId,
        Guid employeeId,
        Guid eventId,
        int revision,
        AttendanceCorrectionKind kind,
        AttendanceEventType? beforeEventType,
        DateTime? beforeOccurredAtUtc,
        AttendanceEventType? afterEventType,
        DateTime? afterOccurredAtUtc)
    {
        return new AttendanceCorrection(
            Guid.NewGuid(),
            recordId,
            employeeId,
            eventId,
            revision,
            kind,
            beforeEventType,
            beforeOccurredAtUtc,
            afterEventType,
            afterOccurredAtUtc,
            "Điều chỉnh kiểm thử",
            Utc(
                20).AddMinutes(
                    revision),
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
