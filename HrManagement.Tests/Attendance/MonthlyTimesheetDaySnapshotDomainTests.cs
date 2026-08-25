using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Tests.Attendance;

public sealed class MonthlyTimesheetDaySnapshotDomainTests
{
    [Fact]
    public void Constructor_PreservesFinalizedAttendanceState()
    {
        Guid periodId =
            Guid.NewGuid();

        Guid attendanceRecordId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var snapshot =
            new MonthlyTimesheetDaySnapshot(
                Guid.NewGuid(),
                periodId,
                attendanceRecordId,
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    21),
                isWorkingDay: true,
                expectedPlannedMinutes: 480,
                AttendanceCalculationStatus.Present,
                workedMinutes: 450,
                lateMinutes: 30,
                earlyLeaveMinutes: 0,
                correctionRevision: 4);

        Assert.Equal(
            periodId,
            snapshot.TimesheetPeriodId);

        Assert.Equal(
            attendanceRecordId,
            snapshot.AttendanceRecordId);

        Assert.Equal(
            employeeId,
            snapshot.EmployeeId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                21),
            snapshot.WorkDate);

        Assert.True(
            snapshot.IsWorkingDay);

        Assert.Equal(
            480,
            snapshot.ExpectedPlannedMinutes);

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            snapshot.Status);

        Assert.Equal(
            450,
            snapshot.WorkedMinutes);

        Assert.Equal(
            30,
            snapshot.LateMinutes);

        Assert.Equal(
            0,
            snapshot.EarlyLeaveMinutes);

        Assert.Equal(
            4,
            snapshot.CorrectionRevision);
    }

    [Theory]
    [InlineData(
        AttendanceCalculationStatus.NotCalculated)]
    [InlineData(
        AttendanceCalculationStatus.Incomplete)]
    public void Constructor_WhenAttendanceIsUnresolved_Throws(
        AttendanceCalculationStatus status)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new MonthlyTimesheetDaySnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        21),
                    isWorkingDay: true,
                    expectedPlannedMinutes: 480,
                    status,
                    workedMinutes: 0,
                    lateMinutes: 0,
                    earlyLeaveMinutes: 0,
                    correctionRevision: 0));
    }

    [Fact]
    public void Constructor_WhenCorrectionRevisionIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new MonthlyTimesheetDaySnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        21),
                    isWorkingDay: true,
                    expectedPlannedMinutes: 480,
                    AttendanceCalculationStatus.Present,
                    workedMinutes: 480,
                    lateMinutes: 0,
                    earlyLeaveMinutes: 0,
                    correctionRevision: -1));
    }

    [Fact]
    public void Constructor_AllowsZeroCorrectionRevision()
    {
        var snapshot =
            new MonthlyTimesheetDaySnapshot(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    21),
                isWorkingDay: true,
                expectedPlannedMinutes: 480,
                AttendanceCalculationStatus.Present,
                workedMinutes: 480,
                lateMinutes: 0,
                earlyLeaveMinutes: 0,
                correctionRevision: 0);

        Assert.Equal(
            0,
            snapshot.CorrectionRevision);
    }
}
