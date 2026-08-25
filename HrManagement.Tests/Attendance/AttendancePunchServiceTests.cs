using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Expectations;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePunchServiceTests
{
    [Fact]
    public async Task FirstClockIn_CreatesRecordAndEvent()
    {
        TestContext test =
            CreateContext();

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        8,
                        20,
                        1,
                        0)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            result.WorkDate);

        Assert.NotNull(
            test.Persistence.NewRecord);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            test.Persistence.NewEvent!.EventType);

        Assert.Null(
            test.Persistence.ExpectedLastEvent);
    }

    [Fact]
    public async Task ClockOut_WithOpenClockIn_AppendsToSameRecord()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateRecord(
                test);

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    15,
                    0));

        test.RecordRepository.ById =
            record;

        test.EventRepository.Latest =
            clockIn;

        test.EventRepository.ByRecord =
            [clockIn];

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockOut,
                    Utc(
                        2026,
                        8,
                        20,
                        23,
                        17)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            record.Id,
            result.AttendanceRecordId);

        Assert.Equal(
            record.WorkDate,
            result.WorkDate);

        Assert.Null(
            test.Persistence.NewRecord);

        Assert.Equal(
            record.Id,
            test.Persistence.NewEvent!.AttendanceRecordId);

        Assert.Equal(
            AttendanceEventType.ClockOut,
            test.Persistence.NewEvent.EventType);

        Assert.Equal(
            clockIn.Id,
            test.Persistence.ExpectedLastEvent!.Id);

        Assert.Equal(
            0,
            test.ContextResolver.CallCount);
    }

    [Fact]
    public async Task ClockOut_WithoutOpenClockIn_IsRejected()
    {
        TestContext test =
            CreateContext();

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockOut,
                    Utc(
                        2026,
                        8,
                        20,
                        10,
                        0)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.NewEvent);

        Assert.Equal(
            0,
            test.ContextResolver.CallCount);
    }

    [Fact]
    public async Task RepeatedClockIn_IsRejected()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateRecord(
                test);

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        test.RecordRepository.ById =
            record;

        test.EventRepository.Latest =
            clockIn;

        test.EventRepository.ByRecord =
            [clockIn];

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        8,
                        20,
                        2,
                        0)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.NewEvent);
    }

    [Fact]
    public async Task SecondClockInSameWorkDate_ReusesExistingRecord()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateRecord(
                test);

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        AttendanceEvent clockOut =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockOut,
                Utc(
                    2026,
                    8,
                    20,
                    5,
                    0));

        test.RecordRepository.ByEmployeeAndDate =
            record;

        test.EventRepository.Latest =
            clockOut;

        test.EventRepository.ByRecord =
            [
                clockIn,
                clockOut
            ];

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        8,
                        20,
                        6,
                        0)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            record.Id,
            result.AttendanceRecordId);

        Assert.Null(
            test.Persistence.NewRecord);

        Assert.Equal(
            clockOut.Id,
            test.Persistence.ExpectedLastEvent!.Id);
    }

    [Fact]
    public async Task OpenOvernightClockIn_LateClockOut_DoesNotResolveNewWorkDate()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord overnightRecord =
            CreateRecord(
                test,
                workDate:
                    new DateOnly(
                        2026,
                        8,
                        20),
                startTime:
                    new TimeOnly(
                        22,
                        0),
                endTime:
                    new TimeOnly(
                        6,
                        0));

        AttendanceEvent clockIn =
            CreateEvent(
                overnightRecord,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    15,
                    0));

        test.RecordRepository.ById =
            overnightRecord;

        test.EventRepository.Latest =
            clockIn;

        test.EventRepository.ByRecord =
            [clockIn];

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockOut,
                    Utc(
                        2026,
                        8,
                        20,
                        23,
                        30)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            result.WorkDate);

        Assert.Equal(
            0,
            test.ContextResolver.CallCount);
    }

    [Fact]
    public async Task ContextResolutionFailure_DoesNotPersist()
    {
        TestContext test =
            CreateContext();

        test.ContextResolver.Result =
            new AttendancePunchContextResolutionResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Không tìm thấy phân lịch.");

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        8,
                        20,
                        1,
                        0)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.NewRecord);

        Assert.Null(
            test.Persistence.NewEvent);
    }

    [Fact]
    public async Task OpenRecordHistoryChanged_IsRejected()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateRecord(
                test);

        AttendanceEvent repositoryLatest =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        AttendanceEvent actualLatest =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    2,
                    0));

        test.RecordRepository.ById =
            record;

        test.EventRepository.Latest =
            repositoryLatest;

        test.EventRepository.ByRecord =
            [
                repositoryLatest,
                actualLatest
            ];

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockOut,
                    Utc(
                        2026,
                        8,
                        20,
                        3,
                        0)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.NewEvent);
    }

    [Fact]
    public async Task FirstClockIn_CapturesExpectationProvenance()
    {
        TestContext test =
            CreateContext();

        Guid holidayId =
            Guid.NewGuid();

        test.ContextResolver.Result =
            new AttendancePunchContextResolutionResult(
                IsSuccessful: true,
                Context:
                    new AttendancePunchContext(
                        test.EmployeeId,
                        test.EmploymentPeriodId,
                        test.AssignmentId,
                        test.ScheduleId,
                        new DateOnly(
                            2026,
                            9,
                            2),
                        "SE Asia Standard Time",
                        false,
                        null,
                        null,
                        0,
                        WorkExpectationSource.Holiday,
                        holidayId,
                        "Quốc khánh"));

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        9,
                        2,
                        1,
                        0)));

        Assert.True(
            result.IsSuccessful);

        AttendanceRecord record =
            Assert.IsType<AttendanceRecord>(
                test.Persistence.NewRecord);

        Assert.False(
            record.IsWorkingDay);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            record.ExpectationSource);

        Assert.Equal(
            holidayId,
            record.ExpectationSourceId);

        Assert.Equal(
            "Quốc khánh",
            record.ExpectationSourceName);
    }

    [Fact]
    public async Task ClockIn_WhenExistingRecordHasOlderExpectationSnapshot_ReusesRecordWithoutRewritingIt()
    {
        TestContext test =
            CreateContext();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        Guid holidayId =
            Guid.NewGuid();

        var existingRecord =
            new AttendanceRecord(
                Guid.NewGuid(),
                test.EmployeeId,
                test.EmploymentPeriodId,
                test.AssignmentId,
                test.ScheduleId,
                workDate,
                "SE Asia Standard Time",
                false,
                expectationSource:
                    WorkExpectationSource.Holiday,
                expectationSourceId:
                    holidayId,
                expectationSourceName:
                    "Quốc khánh");

        test.RecordRepository.ByEmployeeAndDate =
            existingRecord;

        Guid newOverrideId =
            Guid.NewGuid();

        test.ContextResolver.Result =
            new AttendancePunchContextResolutionResult(
                IsSuccessful: true,
                Context:
                    new AttendancePunchContext(
                        test.EmployeeId,
                        test.EmploymentPeriodId,
                        test.AssignmentId,
                        test.ScheduleId,
                        workDate,
                        "SE Asia Standard Time",
                        true,
                        new TimeOnly(
                            22,
                            0),
                        new TimeOnly(
                            6,
                            0),
                        30,
                        WorkExpectationSource.DateOverride,
                        newOverrideId,
                        "Trực thay thế"));

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        9,
                        2,
                        15,
                        0)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            existingRecord.Id,
            result.AttendanceRecordId);

        Assert.Equal(
            workDate,
            result.WorkDate);

        Assert.Null(
            test.Persistence.NewRecord);

        Assert.NotNull(
            test.Persistence.NewEvent);

        Assert.Equal(
            existingRecord.Id,
            test.Persistence.NewEvent!.AttendanceRecordId);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            existingRecord.ExpectationSource);

        Assert.Equal(
            holidayId,
            existingRecord.ExpectationSourceId);

        Assert.Equal(
            "Quốc khánh",
            existingRecord.ExpectationSourceName);

        Assert.False(
            existingRecord.IsWorkingDay);

        Assert.Null(
            existingRecord.ExpectedStartTime);

        Assert.Null(
            existingRecord.ExpectedEndTime);
    }

    [Fact]
    public async Task FirstClockIn_WhenPeriodIsClosed_IsRejectedBeforePersistence()
    {
        TestContext test =
            CreateContext();

        test.PeriodLockPolicy.IsLocked =
            true;

        DateOnly expectedWorkDate =
            new(
                2026,
                8,
                20);

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockIn,
                    Utc(
                        2026,
                        8,
                        20,
                        1,
                        0)));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể ghi nhận chấm công.",
            result.ErrorMessage);

        Assert.Equal(
            1,
            test.ContextResolver.CallCount);

        Assert.Equal(
            1,
            test.PeriodLockPolicy.CallCount);

        Assert.Equal(
            expectedWorkDate,
            test.PeriodLockPolicy.LastWorkDate);

        Assert.Null(
            test.Persistence.NewRecord);

        Assert.Null(
            test.Persistence.NewEvent);
    }

    [Fact]
    public async Task ClockOut_WhenOpenRecordPeriodIsClosed_UsesRecordWorkDateAndRejects()
    {
        TestContext test =
            CreateContext();

        DateOnly recordWorkDate =
            new(
                2026,
                8,
                20);

        AttendanceRecord record =
            CreateRecord(
                test,
                workDate:
                    recordWorkDate,
                startTime:
                    new TimeOnly(
                        22,
                        0),
                endTime:
                    new TimeOnly(
                        6,
                        0));

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                test.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    15,
                    0));

        test.RecordRepository.ById =
            record;

        test.EventRepository.Latest =
            clockIn;

        test.EventRepository.ByRecord =
            [clockIn];

        test.PeriodLockPolicy.IsLocked =
            true;

        RecordAttendancePunchResult result =
            await test.Service.RecordAsync(
                new RecordAttendancePunchRequest(
                    test.EmployeeId,
                    AttendanceEventType.ClockOut,
                    Utc(
                        2026,
                        8,
                        21,
                        0,
                        30)));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể ghi nhận chấm công.",
            result.ErrorMessage);

        Assert.Equal(
            1,
            test.PeriodLockPolicy.CallCount);

        Assert.Equal(
            recordWorkDate,
            test.PeriodLockPolicy.LastWorkDate);

        Assert.Equal(
            0,
            test.ContextResolver.CallCount);

        Assert.Null(
            test.Persistence.NewEvent);
    }

    private static TestContext CreateContext()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        var recordRepository =
            new StubAttendanceRecordRepository();

        var eventRepository =
            new StubAttendanceEventRepository();

        var contextResolver =
            new StubAttendancePunchContextResolver(
                new AttendancePunchContextResolutionResult(
                    IsSuccessful: true,
                    Context:
                        new AttendancePunchContext(
                            employeeId,
                            periodId,
                            assignmentId,
                            scheduleId,
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
                                60,
                            WorkExpectationSource.WeeklySchedule,
                                Guid.NewGuid(),
                                null)));

        var persistence =
            new StubAttendancePunchPersistence();

        var periodLockPolicy =
            new StubAttendancePeriodLockPolicy();

        var service =
            new AttendancePunchService(
                recordRepository,
                eventRepository,
                contextResolver,
                persistence,
                periodLockPolicy);

        return new TestContext(
            employeeId,
            periodId,
            assignmentId,
            scheduleId,
            service,
            recordRepository,
            eventRepository,
            contextResolver,
            persistence,
            periodLockPolicy);
    }

    private static AttendanceRecord CreateRecord(
        TestContext test,
        DateOnly? workDate = null,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null)
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            test.EmployeeId,
            test.EmploymentPeriodId,
            test.AssignmentId,
            test.ScheduleId,
            workDate ??
                new DateOnly(
                    2026,
                    8,
                    20),
            "SE Asia Standard Time",
            true,
            startTime ??
                new TimeOnly(
                    8,
                    0),
            endTime ??
                new TimeOnly(
                    17,
                    0),
            60);
    }

    private static AttendanceEvent CreateEvent(
        AttendanceRecord record,
        Guid employeeId,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            Guid.NewGuid(),
            record.Id,
            employeeId,
            eventType,
            occurredAtUtc);
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

    private sealed record TestContext(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid AssignmentId,
        Guid ScheduleId,
        AttendancePunchService Service,
        StubAttendanceRecordRepository RecordRepository,
        StubAttendanceEventRepository EventRepository,
        StubAttendancePunchContextResolver ContextResolver,
        StubAttendancePunchPersistence Persistence,
        StubAttendancePeriodLockPolicy PeriodLockPolicy);

    private sealed class StubAttendanceRecordRepository
        : IAttendanceRecordRepository
    {
        public AttendanceRecord? ById
        {
            get;
            set;
        }

        public AttendanceRecord? ByEmployeeAndDate
        {
            get;
            set;
        }

        public Task<AttendanceRecord?> GetByIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
        {
            AttendanceRecord? result =
                ById?.Id ==
                    attendanceRecordId
                    ? ById
                    : null;

            return Task.FromResult(
                result);
        }

        public Task<AttendanceRecord?>
            GetByEmployeeAndWorkDateAsync(
                Guid employeeId,
                DateOnly workDate,
                CancellationToken cancellationToken = default)
        {
            AttendanceRecord? result =
                ByEmployeeAndDate is not null
                && ByEmployeeAndDate.EmployeeId ==
                    employeeId
                && ByEmployeeAndDate.WorkDate ==
                    workDate
                    ? ByEmployeeAndDate
                    : null;

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubAttendanceEventRepository
        : IAttendanceEventRepository
    {
        public AttendanceEvent? Latest
        {
            get;
            set;
        }

        public IReadOnlyList<AttendanceEvent> ByRecord
        {
            get;
            set;
        } = [];

        public Task<IReadOnlyList<AttendanceEvent>>
            GetByAttendanceRecordIdAsync(
                Guid attendanceRecordId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AttendanceEvent> result =
                ByRecord
                    .Where(
                        attendanceEvent =>
                            attendanceEvent.AttendanceRecordId ==
                            attendanceRecordId)
                    .OrderBy(
                        attendanceEvent =>
                            attendanceEvent.OccurredAtUtc)
                    .ThenBy(
                        attendanceEvent =>
                            attendanceEvent.Id)
                    .ToList();

            return Task.FromResult(
                result);
        }

        public Task<AttendanceEvent?> GetLatestByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            AttendanceEvent? result =
                Latest?.EmployeeId ==
                    employeeId
                    ? Latest
                    : null;

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubAttendancePunchContextResolver
        : IAttendancePunchContextResolver
    {
        public AttendancePunchContextResolutionResult Result
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public StubAttendancePunchContextResolver(
            AttendancePunchContextResolutionResult result)
        {
            Result =
                result;
        }

        public Task<AttendancePunchContextResolutionResult>
            ResolveAsync(
                Guid employeeId,
                DateTime occurredAtUtc,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubAttendancePunchPersistence
        : IAttendancePunchPersistence
    {
        public AttendanceRecord? NewRecord
        {
            get;
            private set;
        }

        public AttendanceEvent? NewEvent
        {
            get;
            private set;
        }

        public AttendanceEvent? ExpectedLastEvent
        {
            get;
            private set;
        }

        public Task AppendAsync(
            AttendanceRecord? newRecord,
            AttendanceEvent newEvent,
            AttendanceEvent? expectedLastEvent,
            CancellationToken cancellationToken = default)
        {
            NewRecord =
                newRecord;

            NewEvent =
                newEvent;

            ExpectedLastEvent =
                expectedLastEvent;

            return Task.CompletedTask;
        }
    }
}
