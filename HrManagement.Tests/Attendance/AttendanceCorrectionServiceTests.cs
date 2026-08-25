using HrManagement.Application.Attendance.Corrections;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Authentication;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceCorrectionServiceTests
{
    [Fact]
    public async Task ApplyAsync_AddEvent_DerivesLedgerMetadataAndPersists()
    {
        TestContext test =
            CreateContext(
                rawEvents: []);

        var request =
            new ApplyAttendanceCorrectionRequest(
                test.Record.Id,
                AttendanceCorrectionKind.AddEvent,
                AffectedEventId: null,
                AfterEventType:
                    AttendanceEventType.ClockIn,
                AfterOccurredAtUtc:
                    Utc(8),
                Reason:
                    "Bổ sung chấm vào");

        ApplyAttendanceCorrectionResult result =
            await test.Service
                .ApplyAsync(
                    request);

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.AttendanceCorrectionId);

        Assert.NotNull(
            result.AffectedEventId);

        Assert.NotEqual(
            Guid.Empty,
            result.AffectedEventId!.Value);

        Assert.Equal(
            1,
            result.Revision);

        AttendanceCorrection persisted =
            Assert.Single(
                test.Persistence.Appended);

        Assert.Equal(
            test.Record.Id,
            persisted.AttendanceRecordId);

        Assert.Equal(
            test.Record.EmployeeId,
            persisted.EmployeeId);

        Assert.Equal(
            result.AffectedEventId,
            persisted.AffectedEventId);

        Assert.Equal(
            1,
            persisted.Revision);

        Assert.Equal(
            AttendanceCorrectionKind.AddEvent,
            persisted.Kind);

        Assert.Null(
            persisted.BeforeEventType);

        Assert.Null(
            persisted.BeforeOccurredAtUtc);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            persisted.AfterEventType);

        Assert.Equal(
            Utc(8),
            persisted.AfterOccurredAtUtc);

        Assert.Equal(
            Utc(12),
            persisted.CorrectedAtUtc);

        Assert.Equal(
            "user-1",
            persisted.ActorUserId);

        Assert.Equal(
            "admin",
            persisted.ActorUsername);
    }

    [Fact]
    public async Task ApplyAsync_ChangeEvent_DerivesBeforeStateFromEffectiveTimeline()
    {
        Guid clockInId =
            Guid.NewGuid();

        AttendanceRecord record =
            CreateRecord();

        AttendanceEvent clockIn =
            Event(
                record,
                clockInId,
                AttendanceEventType.ClockIn,
                Utc(8));

        AttendanceEvent clockOut =
            Event(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(17));

        TestContext test =
            CreateContext(
                record,
                [
                    clockIn,
                    clockOut
                ]);

        var request =
            new ApplyAttendanceCorrectionRequest(
                record.Id,
                AttendanceCorrectionKind.ChangeEvent,
                AffectedEventId:
                    clockInId,
                AfterEventType:
                    AttendanceEventType.ClockIn,
                AfterOccurredAtUtc:
                    Utc(
                        8,
                        15),
                Reason:
                    "Sửa giờ chấm vào");

        ApplyAttendanceCorrectionResult result =
            await test.Service
                .ApplyAsync(
                    request);

        Assert.True(
            result.IsSuccessful);

        AttendanceCorrection persisted =
            Assert.Single(
                test.Persistence.Appended);

        Assert.Equal(
            clockInId,
            persisted.AffectedEventId);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            persisted.BeforeEventType);

        Assert.Equal(
            Utc(8),
            persisted.BeforeOccurredAtUtc);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            persisted.AfterEventType);

        Assert.Equal(
            Utc(
                8,
                15),
            persisted.AfterOccurredAtUtc);

        Assert.Equal(
            1,
            persisted.Revision);
    }

    [Fact]
    public async Task ApplyAsync_VoidEvent_DerivesBeforeStateAndHasNoAfterState()
    {
        AttendanceRecord record =
            CreateRecord();

        AttendanceEvent clockIn =
            Event(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(8));

        Guid clockOutId =
            Guid.NewGuid();

        AttendanceEvent clockOut =
            Event(
                record,
                clockOutId,
                AttendanceEventType.ClockOut,
                Utc(17));

        TestContext test =
            CreateContext(
                record,
                [
                    clockIn,
                    clockOut
                ]);

        var request =
            new ApplyAttendanceCorrectionRequest(
                record.Id,
                AttendanceCorrectionKind.VoidEvent,
                AffectedEventId:
                    clockOutId,
                AfterEventType: null,
                AfterOccurredAtUtc: null,
                Reason:
                    "Hủy chấm ra sai");

        ApplyAttendanceCorrectionResult result =
            await test.Service
                .ApplyAsync(
                    request);

        Assert.True(
            result.IsSuccessful);

        AttendanceCorrection persisted =
            Assert.Single(
                test.Persistence.Appended);

        Assert.Equal(
            clockOutId,
            persisted.AffectedEventId);

        Assert.Equal(
            AttendanceEventType.ClockOut,
            persisted.BeforeEventType);

        Assert.Equal(
            Utc(17),
            persisted.BeforeOccurredAtUtc);

        Assert.Null(
            persisted.AfterEventType);

        Assert.Null(
            persisted.AfterOccurredAtUtc);

        Assert.Equal(
            AttendanceCorrectionKind.VoidEvent,
            persisted.Kind);
    }

    [Fact]
    public async Task ApplyAsync_WhenCandidateBreaksPunchSequence_RejectsWithoutPersisting()
    {
        AttendanceRecord record =
            CreateRecord();

        Guid clockInId =
            Guid.NewGuid();

        AttendanceEvent clockIn =
            Event(
                record,
                clockInId,
                AttendanceEventType.ClockIn,
                Utc(8));

        AttendanceEvent clockOut =
            Event(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(17));

        TestContext test =
            CreateContext(
                record,
                [
                    clockIn,
                    clockOut
                ]);

        var request =
            new ApplyAttendanceCorrectionRequest(
                record.Id,
                AttendanceCorrectionKind.ChangeEvent,
                AffectedEventId:
                    clockInId,
                AfterEventType:
                    AttendanceEventType.ClockOut,
                AfterOccurredAtUtc:
                    Utc(
                        8,
                        15),
                Reason:
                    "Điều chỉnh kiểm thử");

        ApplyAttendanceCorrectionResult result =
            await test.Service
                .ApplyAsync(
                    request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Lịch sử chấm công phải bắt đầu bằng ClockIn.",
            result.ErrorMessage);

        Assert.Empty(
            test.Persistence.Appended);
    }

    [Fact]
    public async Task ApplyAsync_WhenAuthorizationDenied_DoesNotAppendCorrection()
    {
        TestContext test =
            CreateContext(
                rawEvents: []);

        test.AuthorizationPolicy.IsAuthorized =
            false;

        var request =
            new ApplyAttendanceCorrectionRequest(
                test.Record.Id,
                AttendanceCorrectionKind.AddEvent,
                AffectedEventId: null,
                AfterEventType:
                    AttendanceEventType.ClockIn,
                AfterOccurredAtUtc:
                    Utc(8),
                Reason:
                    "Bổ sung chấm vào");

        ApplyAttendanceCorrectionResult result =
            await test.Service
                .ApplyAsync(
                    request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Bạn không có quyền điều chỉnh chấm công.",
            result.ErrorMessage);

        Assert.Empty(
            test.Persistence.Appended);

        AttendanceCorrectionAuthorizationRequest authorizationRequest =
            Assert.IsType<AttendanceCorrectionAuthorizationRequest>(
                test.AuthorizationPolicy.Request);

        Assert.Equal(
            test.Record.Id,
            authorizationRequest.AttendanceRecordId);

        Assert.Equal(
            test.Record.EmployeeId,
            authorizationRequest.EmployeeId);

        Assert.Equal(
            AttendanceCorrectionKind.AddEvent,
            authorizationRequest.Kind);

        Assert.Equal(
            "user-1",
            authorizationRequest.Actor.UserId);

        Assert.Equal(
            "admin",
            authorizationRequest.Actor.Username);
    }

    [Fact]
    public async Task ApplyAsync_WhenAuthorizationAllowed_AppendsCorrection()
    {
        TestContext test =
            CreateContext(
                rawEvents: []);

        test.AuthorizationPolicy.IsAuthorized =
            true;

        var request =
            new ApplyAttendanceCorrectionRequest(
                test.Record.Id,
                AttendanceCorrectionKind.AddEvent,
                AffectedEventId: null,
                AfterEventType:
                    AttendanceEventType.ClockIn,
                AfterOccurredAtUtc:
                    Utc(8),
                Reason:
                    "Bổ sung chấm vào");

        ApplyAttendanceCorrectionResult result =
            await test.Service
                .ApplyAsync(
                    request);

        Assert.True(
            result.IsSuccessful);

        Assert.Single(
            test.Persistence.Appended);

        Assert.NotNull(
            test.AuthorizationPolicy.Request);

        Assert.Equal(
            test.Record.Id,
            test.AuthorizationPolicy
                .Request!
                .AttendanceRecordId);

        Assert.Equal(
            AttendanceCorrectionKind.AddEvent,
            test.AuthorizationPolicy
                .Request
                .Kind);
    }

    [Fact]
    public async Task ApplyAsync_WhenPeriodIsClosed_RejectsBeforeAuthorizationAndPersistence()
    {
        TestContext test =
            CreateContext(
                rawEvents: []);

        test.PeriodLockPolicy.IsLocked =
            true;

        var request =
            new ApplyAttendanceCorrectionRequest(
                test.Record.Id,
                AttendanceCorrectionKind.AddEvent,
                AffectedEventId: null,
                AfterEventType:
                    AttendanceEventType.ClockIn,
                AfterOccurredAtUtc:
                    Utc(8),
                Reason:
                    "Bổ sung chấm vào");

        ApplyAttendanceCorrectionResult result =
            await test.Service.ApplyAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể điều chỉnh chấm công.",
            result.ErrorMessage);

        Assert.Equal(
            1,
            test.PeriodLockPolicy.CallCount);

        Assert.Equal(
            test.Record.WorkDate,
            test.PeriodLockPolicy.LastWorkDate);

        Assert.Null(
            test.AuthorizationPolicy.Request);

        Assert.Empty(
            test.Persistence.Appended);
    }

    private static TestContext CreateContext(
        IReadOnlyList<AttendanceEvent> rawEvents)
    {
        AttendanceRecord record =
            CreateRecord();

        return CreateContext(
            record,
            rawEvents);
    }

    private static TestContext CreateContext(
        AttendanceRecord record,
        IReadOnlyList<AttendanceEvent> rawEvents)
    {
        var recordRepository =
            new StubAttendanceRecordRepository(
                record);

        var eventRepository =
            new StubAttendanceEventRepository(
                rawEvents);

        var persistence =
            new StubAttendanceCorrectionPersistence();

        var currentUserContext =
            new StubCurrentUserContext(
                new AuthenticatedUser(
                    "user-1",
                    "admin",
                    "Administrator"));

        var authorizationPolicy =
            new TestAttendanceCorrectionAuthorizationPolicy();

        var periodLockPolicy =
            new StubAttendancePeriodLockPolicy();

        var service =
            new AttendanceCorrectionService(
                recordRepository,
                eventRepository,
                persistence,
                new EffectiveAttendanceTimelineResolver(),
                currentUserContext,
                authorizationPolicy,
                periodLockPolicy,
                new FixedTimeProvider(
                    Utc(12)));

        return new TestContext(
            record,
            service,
            persistence,
            authorizationPolicy,
            periodLockPolicy);
    }

    private static AttendanceRecord CreateRecord()
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
                24),
            "SE Asia Standard Time",
            false);
    }

    private static AttendanceEvent Event(
        AttendanceRecord record,
        Guid eventId,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            eventId,
            record.Id,
            record.EmployeeId,
            eventType,
            occurredAtUtc);
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

    private sealed record TestContext(
        AttendanceRecord Record,
        AttendanceCorrectionService Service,
        StubAttendanceCorrectionPersistence Persistence,
        TestAttendanceCorrectionAuthorizationPolicy AuthorizationPolicy,
        StubAttendancePeriodLockPolicy PeriodLockPolicy);

    private sealed class StubAttendanceRecordRepository
        : IAttendanceRecordRepository
    {
        private readonly AttendanceRecord?
            _record;

        public StubAttendanceRecordRepository(
            AttendanceRecord? record)
        {
            _record =
                record;
        }

        public Task<AttendanceRecord?> GetByIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
        {
            AttendanceRecord? result =
                _record?.Id ==
                    attendanceRecordId
                    ? _record
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
                _record?.EmployeeId ==
                    employeeId
                && _record.WorkDate ==
                    workDate
                    ? _record
                    : null;

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubAttendanceEventRepository
        : IAttendanceEventRepository
    {
        private readonly IReadOnlyList<AttendanceEvent>
            _events;

        public StubAttendanceEventRepository(
            IReadOnlyList<AttendanceEvent> events)
        {
            _events =
                events;
        }

        public Task<IReadOnlyList<AttendanceEvent>>
            GetByAttendanceRecordIdAsync(
                Guid attendanceRecordId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AttendanceEvent> result =
                _events
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
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task<AttendanceEvent?>
            GetLatestByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            AttendanceEvent? result =
                _events
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

    private sealed class StubAttendanceCorrectionPersistence
        : IAttendanceCorrectionPersistence
    {
        public List<AttendanceCorrection> Existing
        {
            get;
        } =
            [];

        public List<AttendanceCorrection> Appended
        {
            get;
        } =
            [];

        public Task<IReadOnlyList<AttendanceCorrection>>
            GetByAttendanceRecordIdAsync(
                Guid attendanceRecordId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AttendanceCorrection> result =
                Existing
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
            Appended.Add(
                correction);

            return Task.CompletedTask;
        }
    }

    private sealed class StubCurrentUserContext
        : ICurrentUserContext
    {
        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;

        public StubCurrentUserContext(
            AuthenticatedUser? currentUser)
        {
            CurrentUser =
                currentUser;
        }
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTime utcNow)
        {
            _utcNow =
                new DateTimeOffset(
                    utcNow);
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed class TestAttendanceCorrectionAuthorizationPolicy
    : IAttendanceCorrectionAuthorizationPolicy
    {
        public bool IsAuthorized
        {
            get;
            set;
        } = true;

        public AttendanceCorrectionAuthorizationRequest? Request
        {
            get;
            private set;
        }

        public Task<bool> CanApplyAsync(
            AttendanceCorrectionAuthorizationRequest request,
            CancellationToken cancellationToken = default)
        {
            Request =
                request;

            return Task.FromResult(
                IsAuthorized);
        }
    }
}
