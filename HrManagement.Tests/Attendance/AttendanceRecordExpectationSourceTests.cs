using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceRecordExpectationSourceTests
{
    [Fact]
    public void Constructor_WithoutExplicitSource_DefaultsToWeeklySchedule()
    {
        AttendanceRecord record =
            CreateWorkingRecord();

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            record.ExpectationSource);

        Assert.Null(
            record.ExpectationSourceId);

        Assert.Null(
            record.ExpectationSourceName);
    }

    [Fact]
    public void Constructor_WithWeeklySourceId_CapturesProvenance()
    {
        Guid sourceId =
            Guid.NewGuid();

        AttendanceRecord record =
            CreateWorkingRecord(
                expectationSource:
                    WorkExpectationSource.WeeklySchedule,
                expectationSourceId:
                    sourceId);

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            record.ExpectationSource);

        Assert.Equal(
            sourceId,
            record.ExpectationSourceId);
    }

    [Fact]
    public void Constructor_WithHolidaySource_CapturesTrimmedSnapshot()
    {
        Guid sourceId =
            Guid.NewGuid();

        AttendanceRecord record =
            CreateNonWorkingRecord(
                expectationSource:
                    WorkExpectationSource.Holiday,
                expectationSourceId:
                    sourceId,
                expectationSourceName:
                    "  Quốc khánh  ");

        Assert.Equal(
            WorkExpectationSource.Holiday,
            record.ExpectationSource);

        Assert.Equal(
            sourceId,
            record.ExpectationSourceId);

        Assert.Equal(
            "Quốc khánh",
            record.ExpectationSourceName);

        Assert.False(
            record.IsWorkingDay);
    }

    [Fact]
    public void Constructor_WithDateOverrideSource_CapturesOptionalNote()
    {
        Guid sourceId =
            Guid.NewGuid();

        AttendanceRecord record =
            CreateWorkingRecord(
                expectationSource:
                    WorkExpectationSource.DateOverride,
                expectationSourceId:
                    sourceId,
                expectationSourceName:
                    "  Trực ngày lễ  ");

        Assert.Equal(
            WorkExpectationSource.DateOverride,
            record.ExpectationSource);

        Assert.Equal(
            sourceId,
            record.ExpectationSourceId);

        Assert.Equal(
            "Trực ngày lễ",
            record.ExpectationSourceName);
    }

    [Fact]
    public void Constructor_WithUndefinedSource_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateWorkingRecord(
                    expectationSource:
                        (WorkExpectationSource)999));
    }

    [Fact]
    public void Constructor_WithHolidayWithoutSourceId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateNonWorkingRecord(
                    expectationSource:
                        WorkExpectationSource.Holiday,
                    expectationSourceName:
                        "Quốc khánh"));
    }

    [Fact]
    public void Constructor_WithHolidayWithoutName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateNonWorkingRecord(
                    expectationSource:
                        WorkExpectationSource.Holiday,
                    expectationSourceId:
                        Guid.NewGuid()));
    }

    private static AttendanceRecord CreateWorkingRecord(
        WorkExpectationSource expectationSource =
            WorkExpectationSource.WeeklySchedule,
        Guid? expectationSourceId = null,
        string? expectationSourceName = null)
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                2),
            "SE Asia Standard Time",
            true,
            new TimeOnly(
                8,
                0),
            new TimeOnly(
                17,
                0),
            60,
            expectationSource,
            expectationSourceId,
            expectationSourceName);
    }

    private static AttendanceRecord CreateNonWorkingRecord(
        WorkExpectationSource expectationSource =
            WorkExpectationSource.WeeklySchedule,
        Guid? expectationSourceId = null,
        string? expectationSourceName = null)
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                2),
            "SE Asia Standard Time",
            false,
            expectationSource:
                expectationSource,
            expectationSourceId:
                expectationSourceId,
            expectationSourceName:
                expectationSourceName);
    }
}
