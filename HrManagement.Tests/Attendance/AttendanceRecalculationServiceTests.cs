using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Corrections;
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

    [Fact]
    public async Task WorkingDayWithoutPunches_WithApprovedLeave_BecomesApprovedLeave()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.ApprovedLeaveResolver.Input =
            new ApprovedLeaveAttendanceInput(
                Guid.NewGuid(),
                Guid.NewGuid());

        RecalculateAttendanceResult result =
            await test.Service.RecalculateAsync(
                new RecalculateAttendanceRequest(
                    record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            AttendanceCalculationStatus.ApprovedLeave,
            result.Status);

        Assert.Equal(
            0,
            result.WorkedMinutes);

        Assert.Equal(
            0,
            result.LateMinutes);

        Assert.Equal(
            0,
            result.EarlyLeaveMinutes);

        Assert.Equal(
            1,
            test.ApprovedLeaveResolver.CallCount);

        Assert.Equal(
            record.EmployeeId,
            test.ApprovedLeaveResolver.EmployeeId);

        Assert.Equal(
            record.EmploymentPeriodId,
            test.ApprovedLeaveResolver.EmploymentPeriodId);

        Assert.Equal(
            record.WorkDate,
            test.ApprovedLeaveResolver.WorkDate);

        Assert.Equal(
            0,
            test.ScheduleWindowResolver.CallCount);

        Assert.Equal(
            AttendanceCalculationStatus.ApprovedLeave,
            test.Persistence.Record!.Status);
    }

    [Fact]
    public async Task ApprovedLeaveWithPunches_PreservesActualAttendance()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        test.ApprovedLeaveResolver.Input =
            new ApprovedLeaveAttendanceInput(
                Guid.NewGuid(),
                Guid.NewGuid());

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
                17,
                0))
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
            540,
            result.WorkedMinutes);

        Assert.Equal(
            1,
            test.ScheduleWindowResolver.CallCount);
    }

    [Fact]
    public async Task Correction_ChangesEffectiveTimelineAndCalculatedWorkedMinutes()
    {
        TestContext test =
            CreateContext();

        AttendanceRecord record =
            CreateWorkingRecord();

        test.RecordRepository.Record =
            record;

        Guid clockInId =
            Guid.NewGuid();

        AttendanceEvent clockIn =
            new AttendanceEvent(
                clockInId,
                record.Id,
                record.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        AttendanceEvent clockOut =
            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    17,
                    0));

        test.EventRepository.Events =
        [
            clockIn,
        clockOut
        ];

        test.CorrectionPersistence.Corrections =
        [
            new AttendanceCorrection(
            Guid.NewGuid(),
            record.Id,
            record.EmployeeId,
            clockInId,
            revision: 1,
            AttendanceCorrectionKind.ChangeEvent,
            AttendanceEventType.ClockIn,
            Utc(
                8,
                0),
            AttendanceEventType.ClockIn,
            Utc(
                9,
                0),
            "Sửa giờ chấm vào",
            Utc(
                18,
                0),
            "user-1",
            "admin")
        ];

        RecalculateAttendanceResult result =
            await test.Service
                .RecalculateAsync(
                    new RecalculateAttendanceRequest(
                        record.Id));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            480,
            result.WorkedMinutes);

        Assert.Equal(
            1,
            test.Persistence.ExpectedCorrectionRevision);

        Assert.Equal(
            2,
            test.Persistence.ExpectedEvents.Count);

        Assert.Equal(
            Utc(
                8,
                0),
            test.Persistence
                .ExpectedEvents[0]
                .OccurredAtUtc);
    }

    private static TestContext CreateContext(
        AttendanceAdherencePolicy? policy = null)
    {
        var correctionPersistence =
            new StubAttendanceCorrectionPersistence();

        var timelineResolver =
            new EffectiveAttendanceTimelineResolver();

        var approvedLeaveResolver =
            new StubApprovedLeaveAttendanceResolver();

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
                approvedLeaveResolver,
                scheduleWindowResolver,
                persistence,
                policy ??
                    new AttendanceAdherencePolicy(),
                correctionPersistence,
                timelineResolver);

        return new TestContext(
            service,
            recordRepository,
            eventRepository,
            approvedLeaveResolver,
            scheduleWindowResolver,
            persistence,
            correctionPersistence);
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
        StubApprovedLeaveAttendanceResolver ApprovedLeaveResolver,
        StubAttendanceScheduleWindowResolver ScheduleWindowResolver,
        StubAttendanceCalculationPersistence Persistence,
        StubAttendanceCorrectionPersistence CorrectionPersistence);

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

        public int ExpectedCorrectionRevision
        {
            get;
            private set;
        }

        public Task ApplyAsync(
            AttendanceRecord calculatedRecord,
            IReadOnlyList<AttendanceEvent> expectedEvents,
            int expectedCorrectionRevision,
            CancellationToken cancellationToken = default)
        {
            Record =
                calculatedRecord;

            ExpectedEvents =
                expectedEvents.ToList();

            ExpectedCorrectionRevision =
                expectedCorrectionRevision;

            return Task.CompletedTask;
        }
    }

    private sealed class StubApprovedLeaveAttendanceResolver
    : IApprovedLeaveAttendanceResolver
    {
        public ApprovedLeaveAttendanceInput? Input
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Guid? EmployeeId
        {
            get;
            private set;
        }

        public Guid? EmploymentPeriodId
        {
            get;
            private set;
        }

        public DateOnly? WorkDate
        {
            get;
            private set;
        }

        public Task<ApprovedLeaveAttendanceInput?> ResolveAsync(
            Guid employeeId,
            Guid employmentPeriodId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            EmployeeId =
                employeeId;

            EmploymentPeriodId =
                employmentPeriodId;

            WorkDate =
                workDate;

            return Task.FromResult(
                Input);
        }
    }

    private sealed class StubAttendanceCorrectionPersistence
    : IAttendanceCorrectionPersistence
    {
        public IReadOnlyList<AttendanceCorrection> Corrections
        {
            get;
            set;
        } = [];

        public Task<IReadOnlyList<AttendanceCorrection>>
            GetByAttendanceRecordIdAsync(
                Guid attendanceRecordId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AttendanceCorrection> result =
                Corrections
                    .Where(
                        correction =>
                            correction.AttendanceRecordId ==
                            attendanceRecordId)
                    .OrderBy(
                        correction =>
                            correction.Revision)
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task AppendAsync(
            AttendanceCorrection correction,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
