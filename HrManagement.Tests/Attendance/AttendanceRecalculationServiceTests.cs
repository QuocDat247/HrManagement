using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceRecalculationServiceTests
{
    [Fact]
    public async Task CompletedWorkingDay_CalculatesAndPersists()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.EventRepository.Events =
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

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            result.Status);

        Assert.Equal(
            515,
            result.WorkedMinutes);

        Assert.Equal(
            10,
            result.LateMinutes);

        Assert.Equal(
            15,
            result.EarlyLeaveMinutes);

        Assert.Same(
            record,
            test.Persistence.Record);

        Assert.Equal(
            2,
            test.Persistence.ExpectedEvents.Count);
    }

    [Fact]
    public async Task WorkingDayWithoutPunches_BecomesAbsent()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            result.Status);

        Assert.Equal(
            0,
            result.WorkedMinutes);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            test.Persistence.Record!.Status);
    }

    [Fact]
    public async Task OpenClockIn_BecomesIncomplete()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.EventRepository.Events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10))
        ];

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            AttendanceCalculationStatus.Incomplete,
            result.Status);

        Assert.Equal(
            10,
            result.LateMinutes);

        Assert.Equal(
            0,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public async Task NonWorkingDay_PreservesActualWorkedMinutes()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateNonWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.EventRepository.Events =
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

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            AttendanceCalculationStatus.NonWorkingDay,
            result.Status);

        Assert.Equal(
            120,
            result.WorkedMinutes);

        Assert.Equal(
            0,
            result.LateMinutes);

        Assert.Equal(
            0,
            result.EarlyLeaveMinutes);

        Assert.Equal(
            0,
            test.ScheduleWindowResolver.CallCount);
    }

    [Fact]
    public async Task GracePolicy_IsApplied()
    {
        TestContext test =
            CreateContext(
                new AttendanceAdherencePolicy(
                    lateGraceMinutes: 5,
                    earlyLeaveGraceMinutes: 5));

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.EventRepository.Events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    6)),

            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    16,
                    54))
        ];

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            1,
            result.LateMinutes);

        Assert.Equal(
            1,
            result.EarlyLeaveMinutes);
    }

    [Fact]
    public async Task MissingRecord_ReturnsFailureAndDoesNotPersist()
    {
        TestContext test =
            CreateContext();

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    Guid.NewGuid()));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Record);
    }

    [Fact]
    public async Task BrokenTimeline_ReturnsFailureAndDoesNotPersist()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.EventRepository.Events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0)),

            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    9,
                    0))
        ];

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Record);
    }

    [Fact]
    public async Task ScheduleWindowFailure_DoesNotPersist()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.EventRepository.Events =
        [
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0))
        ];

        test.ScheduleWindowResolver.Exception =
            new InvalidOperationException(
                "Không thể resolve schedule window.");

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Record);
    }

    private static TestContext CreateContext(
        AttendanceAdherencePolicy? policy = null)
    {
        var recordRepository =
            new StubAttendanceRecordRepository();

        var eventRepository =
            new StubAttendanceEventRepository();

        var scheduleWindowResolver =
            new StubAttendanceScheduleWindowResolver();

        var persistence =
            new StubAttendanceCalculationPersistence();

        var service =
            new AttendanceRecalculationService(
                recordRepository,
                eventRepository,
                scheduleWindowResolver,
                persistence,
                policy ??
                    new AttendanceAdherencePolicy());

        return new TestContext(
            service,
            recordRepository,
            eventRepository,
            scheduleWindowResolver,
            persistence);
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
            "UTC",
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
            "UTC",
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

    private sealed record TestContext(
        AttendanceRecalculationService Service,
        StubAttendanceRecordRepository RecordRepository,
        StubAttendanceEventRepository EventRepository,
        StubAttendanceScheduleWindowResolver ScheduleWindowResolver,
        StubAttendanceCalculationPersistence Persistence);

    private sealed class StubAttendanceRecordRepository
        : IAttendanceRecordRepository
    {
        public AttendanceRecord? Record
        {
            get;
            set;
        }

        public Task<AttendanceRecord?> GetByIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
        {
            AttendanceRecord? result =
                Record?.Id ==
                    attendanceRecordId
                    ? Record
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
                Record is not null
                && Record.EmployeeId ==
                    employeeId
                && Record.WorkDate ==
                    workDate
                    ? Record
                    : null;

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubAttendanceEventRepository
        : IAttendanceEventRepository
    {
        public IReadOnlyList<AttendanceEvent> Events
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
                Events
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
                Events
                    .Where(
                        attendanceEvent =>
                            attendanceEvent.EmployeeId ==
                            employeeId)
                    .OrderByDescending(
                        attendanceEvent =>
                            attendanceEvent.OccurredAtUtc)
                    .ThenByDescending(
                        attendanceEvent =>
                            attendanceEvent.Id)
                    .FirstOrDefault();

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubAttendanceScheduleWindowResolver
        : IAttendanceScheduleWindowResolver
    {
        public Exception? Exception
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public AttendanceScheduleWindow? Resolve(
            AttendanceRecord record)
        {
            if (!record.IsWorkingDay)
            {
                return null;
            }

            CallCount++;

            if (Exception is not null)
            {
                throw Exception;
            }

            return new AttendanceScheduleWindow(
                Utc(
                    8,
                    0),
                Utc(
                    17,
                    0));
        }
    }

    private sealed class StubAttendanceCalculationPersistence
        : IAttendanceCalculationPersistence
    {
        public AttendanceRecord? Record
        {
            get;
            private set;
        }

        public IReadOnlyList<AttendanceEvent> ExpectedEvents
        {
            get;
            private set;
        } = [];

        public Task ApplyAsync(
            AttendanceRecord calculatedRecord,
            IReadOnlyList<AttendanceEvent> expectedEvents,
            CancellationToken cancellationToken = default)
        {
            Record =
                calculatedRecord;

            ExpectedEvents =
                expectedEvents.ToList();

            return Task.CompletedTask;
        }
    }
}
