using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceRecordCalculationStateTests
{
    [Fact]
    public void NewRecord_StartsNotCalculated()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        Assert.Equal(
            AttendanceCalculationStatus.NotCalculated,
            record.Status);

        Assert.Equal(
            0,
            record.WorkedMinutes);

        Assert.Equal(
            0,
            record.LateMinutes);

        Assert.Equal(
            0,
            record.EarlyLeaveMinutes);
    }

    [Fact]
    public void CompletedWorkingDay_AppliesCalculatedState()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10)),

            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    16,
                    45))
        ];

        DailyAttendanceCalculation daily =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        AttendanceScheduleAdherence adherence =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                new AttendanceScheduleWindow(
                    Utc(
                        8,
                        0),
                    Utc(
                        17,
                        0)),
                new AttendanceAdherencePolicy());

        record.ApplyCalculation(
            daily,
            adherence);

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            record.Status);

        Assert.Equal(
            515,
            record.WorkedMinutes);

        Assert.Equal(
            10,
            record.LateMinutes);

        Assert.Equal(
            15,
            record.EarlyLeaveMinutes);
    }

    [Fact]
    public void AbsentWorkingDay_AppliesAbsentState()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        DailyAttendanceCalculation daily =
            DailyAttendanceCalculator.Calculate(
                record,
                []);

        AttendanceScheduleAdherence adherence =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                scheduleWindow: null,
                new AttendanceAdherencePolicy());

        record.ApplyCalculation(
            daily,
            adherence);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            record.Status);

        Assert.Equal(
            0,
            record.WorkedMinutes);

        Assert.Equal(
            0,
            record.LateMinutes);

        Assert.Equal(
            0,
            record.EarlyLeaveMinutes);
    }

    [Fact]
    public void IncompleteDay_PreservesWorkedAndLateMetrics()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10)),

            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0)),

            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    13,
                    0))
        ];

        DailyAttendanceCalculation daily =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        AttendanceScheduleAdherence adherence =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                new AttendanceScheduleWindow(
                    Utc(
                        8,
                        0),
                    Utc(
                        17,
                        0)),
                new AttendanceAdherencePolicy());

        record.ApplyCalculation(
            daily,
            adherence);

        Assert.Equal(
            AttendanceCalculationStatus.Incomplete,
            record.Status);

        Assert.Equal(
            230,
            record.WorkedMinutes);

        Assert.Equal(
            10,
            record.LateMinutes);

        Assert.Equal(
            0,
            record.EarlyLeaveMinutes);
    }

    [Fact]
    public void NonWorkingDay_PreservesActualWorkedMinutes()
    {
        AttendanceRecord record =
            CreateNonWorkingRecord();

        IReadOnlyList<AttendanceEvent> events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    10,
                    0))
        ];

        DailyAttendanceCalculation daily =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        AttendanceScheduleAdherence adherence =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                scheduleWindow: null,
                new AttendanceAdherencePolicy());

        record.ApplyCalculation(
            daily,
            adherence);

        Assert.Equal(
            AttendanceCalculationStatus.NonWorkingDay,
            record.Status);

        Assert.Equal(
            120,
            record.WorkedMinutes);

        Assert.Equal(
            0,
            record.LateMinutes);

        Assert.Equal(
            0,
            record.EarlyLeaveMinutes);
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
}
