using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePunchContextResolverTests
{
    [Fact]
    public async Task DayShift_ResolvesSameLocalWorkDate()
    {
        TestContext test =
            CreateSingleScheduleContext(
                CreateOfficeDays());

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.Context);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            result.Context!.WorkDate);

        Assert.Equal(
            test.AssignmentId,
            result.Context.WorkScheduleAssignmentId);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            result.Context.ExpectedStartTime);
    }

    [Fact]
    public async Task OvernightShift_AfterMidnight_ResolvesPreviousWorkDate()
    {
        TestContext test =
            CreateSingleScheduleContext(
                CreateNightDays());

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    8,
                    20,
                    18,
                    0));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            result.Context!.WorkDate);

        Assert.True(
            result.Context.ExpectedStartTime >
            result.Context.ExpectedEndTime);
    }

    [Fact]
    public async Task OvernightShift_AtExpectedEnd_StillResolvesPreviousWorkDate()
    {
        TestContext test =
            CreateSingleScheduleContext(
                CreateNightDays());

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    8,
                    20,
                    23,
                    0));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            result.Context!.WorkDate);
    }

    [Fact]
    public async Task OvernightShift_AfterExpectedEnd_ResolvesCurrentDate()
    {
        TestContext test =
            CreateSingleScheduleContext(
                CreateNightDays());

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    8,
                    20,
                    23,
                    1));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                21),
            result.Context!.WorkDate);
    }

    [Fact]
    public async Task ScheduleChangeDuringOvernightShift_UsesPreviousAssignment()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid oldScheduleId =
            Guid.NewGuid();

        Guid newScheduleId =
            Guid.NewGuid();

        Guid oldAssignmentId =
            Guid.NewGuid();

        Guid newAssignmentId =
            Guid.NewGuid();

        var oldAssignment =
            new EmployeeWorkScheduleAssignment(
                oldAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    8,
                    20));

        var newAssignment =
            new EmployeeWorkScheduleAssignment(
                newAssignmentId,
                employeeId,
                periodId,
                newScheduleId,
                new DateOnly(
                    2026,
                    8,
                    21));

        var oldSchedule =
            new WorkSchedule(
                oldScheduleId,
                "NIGHT",
                "Ca đêm",
                "TEST");

        var newSchedule =
            new WorkSchedule(
                newScheduleId,
                "DAY",
                "Ca ngày",
                "TEST");

        var resolver =
            new AttendancePunchContextResolver(
                new StubAssignmentRepository(
                    [
                        oldAssignment,
                        newAssignment
                    ]),
                new StubScheduleRepository(
                    [
                        oldSchedule,
                        newSchedule
                    ]),
                new StubScheduleDayRepository(
                    new Dictionary<Guid, IReadOnlyList<WorkScheduleDay>>
                    {
                        [oldScheduleId] =
                            CreateNightDays(
                                oldScheduleId),

                        [newScheduleId] =
                            CreateOfficeDays(
                                newScheduleId)
                    }),
                new PlusSevenTimeZoneConverter());

        AttendancePunchContextResolutionResult result =
            await resolver.ResolveAsync(
                employeeId,
                Utc(
                    2026,
                    8,
                    20,
                    18,
                    0));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            result.Context!.WorkDate);

        Assert.Equal(
            oldAssignmentId,
            result.Context.WorkScheduleAssignmentId);

        Assert.Equal(
            oldScheduleId,
            result.Context.WorkScheduleId);
    }

    [Fact]
    public async Task NonWorkingDay_ResolvesZeroExpectationContext()
    {
        TestContext test =
            CreateSingleScheduleContext(
                CreateOfficeDays());

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    8,
                    22,
                    1,
                    0));

        Assert.True(
            result.IsSuccessful);

        Assert.False(
            result.Context!.IsWorkingDay);

        Assert.Null(
            result.Context.ExpectedStartTime);

        Assert.Null(
            result.Context.ExpectedEndTime);

        Assert.Equal(
            0,
            result.Context.ExpectedBreakMinutes);
    }

    private static TestContext CreateSingleScheduleContext(
        IReadOnlyList<WorkScheduleDay> days)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid scheduleId =
            days[0].WorkScheduleId;

        Guid assignmentId =
            Guid.NewGuid();

        var schedule =
            new WorkSchedule(
                scheduleId,
                "TEST",
                "Lịch kiểm thử",
                "TEST");

        var assignment =
            new EmployeeWorkScheduleAssignment(
                assignmentId,
                employeeId,
                periodId,
                scheduleId,
                new DateOnly(
                    2026,
                    1,
                    1));

        var resolver =
            new AttendancePunchContextResolver(
                new StubAssignmentRepository(
                    [assignment]),
                new StubScheduleRepository(
                    [schedule]),
                new StubScheduleDayRepository(
                    new Dictionary<Guid, IReadOnlyList<WorkScheduleDay>>
                    {
                        [scheduleId] =
                            days
                    }),
                new PlusSevenTimeZoneConverter());

        return new TestContext(
            employeeId,
            assignmentId,
            resolver);
    }

    private static IReadOnlyList<WorkScheduleDay>
        CreateOfficeDays(
            Guid? scheduleId = null)
    {
        Guid id =
            scheduleId ??
            Guid.NewGuid();

        var result =
            new List<WorkScheduleDay>();

        foreach (DayOfWeek dayOfWeek
                 in Enum.GetValues<DayOfWeek>())
        {
            bool working =
                dayOfWeek is >= DayOfWeek.Monday
                and <= DayOfWeek.Friday;

            result.Add(
                working
                    ? new WorkScheduleDay(
                        Guid.NewGuid(),
                        id,
                        dayOfWeek,
                        true,
                        new TimeOnly(
                            8,
                            0),
                        new TimeOnly(
                            17,
                            0),
                        60)
                    : new WorkScheduleDay(
                        Guid.NewGuid(),
                        id,
                        dayOfWeek,
                        false));
        }

        return result;
    }

    private static IReadOnlyList<WorkScheduleDay>
        CreateNightDays(
            Guid? scheduleId = null)
    {
        Guid id =
            scheduleId ??
            Guid.NewGuid();

        var result =
            new List<WorkScheduleDay>();

        foreach (DayOfWeek dayOfWeek
                 in Enum.GetValues<DayOfWeek>())
        {
            bool working =
                dayOfWeek is >= DayOfWeek.Monday
                and <= DayOfWeek.Friday;

            result.Add(
                working
                    ? new WorkScheduleDay(
                        Guid.NewGuid(),
                        id,
                        dayOfWeek,
                        true,
                        new TimeOnly(
                            22,
                            0),
                        new TimeOnly(
                            6,
                            0),
                        60)
                    : new WorkScheduleDay(
                        Guid.NewGuid(),
                        id,
                        dayOfWeek,
                        false));
        }

        return result;
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
        Guid AssignmentId,
        AttendancePunchContextResolver Resolver);

    private sealed class StubAssignmentRepository
        : IEmployeeWorkScheduleAssignmentRepository
    {
        private readonly IReadOnlyList<
            EmployeeWorkScheduleAssignment>
            _assignments;

        public StubAssignmentRepository(
            IReadOnlyList<EmployeeWorkScheduleAssignment>
                assignments)
        {
            _assignments =
                assignments;
        }

        public Task<IReadOnlyList<EmployeeWorkScheduleAssignment>>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<EmployeeWorkScheduleAssignment> result =
                _assignments
                    .Where(
                        assignment =>
                            assignment.EmployeeId ==
                            employeeId)
                    .ToList();

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubScheduleRepository
        : IWorkScheduleRepository
    {
        private readonly IReadOnlyDictionary<Guid, WorkSchedule>
            _schedules;

        public StubScheduleRepository(
            IEnumerable<WorkSchedule> schedules)
        {
            _schedules =
                schedules.ToDictionary(
                    schedule =>
                        schedule.Id);
        }

        public Task<WorkSchedule?> GetByIdAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            _schedules.TryGetValue(
                workScheduleId,
                out WorkSchedule? schedule);

            return Task.FromResult(
                schedule);
        }
    }

    private sealed class StubScheduleDayRepository
        : IWorkScheduleDayRepository
    {
        private readonly IReadOnlyDictionary<
            Guid,
            IReadOnlyList<WorkScheduleDay>>
            _days;

        public StubScheduleDayRepository(
            IReadOnlyDictionary<
                Guid,
                IReadOnlyList<WorkScheduleDay>> days)
        {
            _days =
                days;
        }

        public Task<IReadOnlyList<WorkScheduleDay>>
            GetByWorkScheduleIdAsync(
                Guid workScheduleId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WorkScheduleDay> result =
                _days.TryGetValue(
                    workScheduleId,
                    out IReadOnlyList<WorkScheduleDay>? days)
                    ? days
                    : [];

            return Task.FromResult(
                result);
        }
    }

    private sealed class PlusSevenTimeZoneConverter
        : IAttendanceTimeZoneConverter
    {
        public DateTime ConvertFromUtc(
            DateTime occurredAtUtc,
            string timeZoneId)
        {
            return DateTime.SpecifyKind(
                occurredAtUtc.AddHours(
                    7),
                DateTimeKind.Unspecified);
        }
    }
}
