using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class DailyAttendanceCalculatorTests
{
    [Fact]
    public void WorkingDayWithoutEvents_IsAbsent()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                []);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            result.Status);

        Assert.Equal(
            0,
            result.WorkedMinutes);

        Assert.Equal(
            0,
            result.CompletedPairCount);

        Assert.Null(
            result.FirstClockInAtUtc);

        Assert.Null(
            result.LastClockOutAtUtc);

        Assert.False(
            result.HasOpenClockIn);
    }

    [Fact]
    public void NonWorkingDayWithoutEvents_IsNonWorkingDay()
    {
        AttendanceRecord record =
            CreateNonWorkingRecord();

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                []);

        Assert.Equal(
            AttendanceCalculationStatus.NonWorkingDay,
            result.Status);

        Assert.Equal(
            0,
            result.WorkedMinutes);
    }

    [Fact]
    public void SingleCompletedPair_IsPresent()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    17,
                    0))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            result.Status);

        Assert.Equal(
            540,
            result.WorkedMinutes);

        Assert.Equal(
            1,
            result.CompletedPairCount);

        Assert.Equal(
            Utc(
                8,
                0),
            result.FirstClockInAtUtc);

        Assert.Equal(
            Utc(
                17,
                0),
            result.LastClockOutAtUtc);

        Assert.False(
            result.HasOpenClockIn);
    }

    [Fact]
    public void MultiplePairs_SumsWorkedMinutes()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    13,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    17,
                    0))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            result.Status);

        Assert.Equal(
            480,
            result.WorkedMinutes);

        Assert.Equal(
            2,
            result.CompletedPairCount);
    }

    [Fact]
    public void OpenClockIn_IsIncomplete()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        Assert.Equal(
            AttendanceCalculationStatus.Incomplete,
            result.Status);

        Assert.Equal(
            0,
            result.WorkedMinutes);

        Assert.Equal(
            0,
            result.CompletedPairCount);

        Assert.True(
            result.HasOpenClockIn);

        Assert.Equal(
            Utc(
                8,
                0),
            result.FirstClockInAtUtc);

        Assert.Null(
            result.LastClockOutAtUtc);
    }

    [Fact]
    public void CompletedPairThenOpenClockIn_PreservesCompletedWork()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    13,
                    0))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        Assert.Equal(
            AttendanceCalculationStatus.Incomplete,
            result.Status);

        Assert.Equal(
            240,
            result.WorkedMinutes);

        Assert.Equal(
            1,
            result.CompletedPairCount);

        Assert.True(
            result.HasOpenClockIn);

        Assert.Equal(
            Utc(
                12,
                0),
            result.LastClockOutAtUtc);
    }

    [Fact]
    public void NonWorkingDayWithCompletedPair_PreservesWorkedMinutes()
    {
        AttendanceRecord record =
            CreateNonWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    10,
                    30))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        Assert.Equal(
            AttendanceCalculationStatus.NonWorkingDay,
            result.Status);

        Assert.Equal(
            150,
            result.WorkedMinutes);

        Assert.Equal(
            1,
            result.CompletedPairCount);
    }

    [Fact]
    public void EventsForDifferentRecord_AreRejected()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        AttendanceEvent foreignEvent =
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                record.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        Assert.Throws<InvalidOperationException>(
            () =>
                DailyAttendanceCalculator.Calculate(
                    record,
                    [foreignEvent]));
    }

    [Fact]
    public void EventsForDifferentEmployee_AreRejected()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        AttendanceEvent foreignEvent =
            new(
                Guid.NewGuid(),
                record.Id,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        Assert.Throws<InvalidOperationException>(
            () =>
                DailyAttendanceCalculator.Calculate(
                    record,
                    [foreignEvent]));
    }

    [Fact]
    public void BrokenPunchSequence_IsRejected()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    9,
                    0))
        ];

        Assert.Throws<InvalidOperationException>(
            () =>
                DailyAttendanceCalculator.Calculate(
                    record,
                    events));
    }

    [Fact]
    public void WorkedSeconds_AreSummedBeforeConvertingToWholeMinutes()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    8,
                    1,
                    40)),

            CreateEvent(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    9,
                    0,
                    0)),

            CreateEvent(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    9,
                    1,
                    40))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        Assert.Equal(
            3,
            result.WorkedMinutes);
    }

    [Fact]
    public void WorkingDayWithoutEvents_WithApprovedLeave_IsApprovedLeave()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                [],
                hasApprovedLeave: true);

        Assert.Equal(
            AttendanceCalculationStatus.ApprovedLeave,
            result.Status);

        Assert.Equal(
            0,
            result.WorkedMinutes);

        Assert.Equal(
            0,
            result.CompletedPairCount);
    }

    [Fact]
    public void NonWorkingDay_WithApprovedLeave_RemainsNonWorkingDay()
    {
        AttendanceRecord record =
            CreateNonWorkingRecord();

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                [],
                hasApprovedLeave: true);

        Assert.Equal(
            AttendanceCalculationStatus.NonWorkingDay,
            result.Status);
    }

    [Fact]
    public void WorkingDayWithPunches_ApprovedLeaveDoesNotHideActualWork()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            CreateEvent(
            record,
            AttendanceEventType.ClockIn,
            Utc(
                8,
                0)),

        CreateEvent(
            record,
            AttendanceEventType.ClockOut,
            Utc(
                17,
                0))
        ];

        DailyAttendanceCalculation result =
            DailyAttendanceCalculator.Calculate(
                record,
                events,
                hasApprovedLeave: true);

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            result.Status);

        Assert.Equal(
            540,
            result.WorkedMinutes);
    }

    private static AttendanceRecord CreateWorkingRecord()
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
                20),
            "SE Asia Standard Time",
            true,
            new TimeOnly(
                8,
                0),
            new TimeOnly(
                17,
                0),
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

    private static AttendanceEvent CreateEvent(
        AttendanceRecord record,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            Guid.NewGuid(),
            record.Id,
            record.EmployeeId,
            eventType,
            occurredAtUtc);
    }

    private static DateTime Utc(
        int hour,
        int minute,
        int second = 0)
    {
        return new DateTime(
            2026,
            8,
            20,
            hour,
            minute,
            second,
            DateTimeKind.Utc);
    }
}
