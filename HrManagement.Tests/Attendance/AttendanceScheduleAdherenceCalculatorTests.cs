using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceScheduleAdherenceCalculatorTests
{
    [Fact]
    public void OnTimeCompletedDay_HasNoLateOrEarlyLeave()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        0)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        10,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy());

        Assert.Equal(
            0,
            result.LateMinutes);

        Assert.Equal(
            0,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public void LateClockIn_CalculatesLateMinutes()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        12)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        10,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy());

        Assert.Equal(
            12,
            result.LateMinutes);
    }

    [Fact]
    public void LateGraceMinutes_AreDeducted()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        6)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        10,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy(
                    lateGraceMinutes: 5));

        Assert.Equal(
            1,
            result.LateMinutes);
    }

    [Fact]
    public void LateWithinGrace_IsZero()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        4)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        10,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy(
                    lateGraceMinutes: 5));

        Assert.Equal(
            0,
            result.LateMinutes);
    }

    [Fact]
    public void EarlyClockOut_CalculatesEarlyLeaveMinutes()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        0)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        9,
                        42)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy());

        Assert.Equal(
            18,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public void EarlyLeaveGraceMinutes_AreDeducted()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        0)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        9,
                        54)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy(
                    earlyLeaveGraceMinutes: 5));

        Assert.Equal(
            1,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public void IncompleteDay_DoesNotTreatPreviousClockOutAsEarlyLeave()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        0)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        5,
                        0)),
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        6,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy());

        Assert.Equal(
            AttendanceCalculationStatus.Incomplete,
            daily.Status);

        Assert.Equal(
            0,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public void NonWorkingDay_AlwaysHasZeroAdherence()
    {
        AttendanceRecord record =
            CreateNonWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        0)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        3,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                scheduleWindow: null,
                new AttendanceAdherencePolicy());

        Assert.Equal(
            0,
            result.LateMinutes);

        Assert.Equal(
            0,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public void CeilingRounding_CountsPartialMinute()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        1,
                        0,
                        1)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        10,
                        0)));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                DayWindow(),
                new AttendanceAdherencePolicy(
                    roundingMode:
                        AttendanceMinuteRoundingMode.Ceiling));

        Assert.Equal(
            1,
            result.LateMinutes);
    }

    [Fact]
    public void OvernightWindow_CalculatesAgainstAbsoluteUtcBoundaries()
    {
        AttendanceRecord record =
            CreateOvernightRecord();

        DailyAttendanceCalculation daily =
            CalculateDaily(
                record,
                Event(
                    record,
                    AttendanceEventType.ClockIn,
                    Utc(
                        15,
                        5)),
                Event(
                    record,
                    AttendanceEventType.ClockOut,
                    Utc(
                        22,
                        50)));

        var window =
            new AttendanceScheduleWindow(
                Utc(
                    15,
                    0),
                Utc(
                    23,
                    0));

        AttendanceScheduleAdherence result =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                window,
                new AttendanceAdherencePolicy());

        Assert.Equal(
            5,
            result.LateMinutes);

        Assert.Equal(
            10,
            result.EarlyLeaveMinutes);
    }

    private static DailyAttendanceCalculation CalculateDaily(
        AttendanceRecord record,
        params AttendanceEvent[] events)
    {
        return DailyAttendanceCalculator.Calculate(
            record,
            events);
    }

    private static AttendanceScheduleWindow DayWindow()
    {
        return new AttendanceScheduleWindow(
            Utc(
                1,
                0),
            Utc(
                10,
                0));
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

    private static AttendanceRecord CreateOvernightRecord()
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
                22,
                0),
            new TimeOnly(
                6,
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

    private static AttendanceEvent Event(
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
