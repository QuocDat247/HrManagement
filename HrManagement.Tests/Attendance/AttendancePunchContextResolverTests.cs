using HrManagement.Application.Attendance.Expectations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePunchContextResolverTests
{
    [Fact]
    public async Task DayShift_ResolvesSameLocalWorkDate()
    {
        TestContext test =
            CreateSingleScheduleContext();

        DateOnly workDate =
            new(
                2026,
                8,
                20);

        test.ExpectationResolver.Set(
            WorkingExpectation(
                test.ScheduleId,
                workDate,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0)));

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
            CreateSingleScheduleContext();

        DateOnly previousWorkDate =
            new(
                2026,
                8,
                20);

        test.ExpectationResolver.Set(
            WorkingExpectation(
                test.ScheduleId,
                previousWorkDate,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0)));

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
            CreateSingleScheduleContext();

        DateOnly previousWorkDate =
            new(
                2026,
                8,
                20);

        test.ExpectationResolver.Set(
            WorkingExpectation(
                test.ScheduleId,
                previousWorkDate,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0)));

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
            CreateSingleScheduleContext();

        DateOnly previousWorkDate =
            new(
                2026,
                8,
                20);

        DateOnly currentWorkDate =
            new(
                2026,
                8,
                21);

        test.ExpectationResolver.Set(
            WorkingExpectation(
                test.ScheduleId,
                previousWorkDate,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0)));

        test.ExpectationResolver.Set(
            WorkingExpectation(
                test.ScheduleId,
                currentWorkDate,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0)));

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

        DateOnly previousWorkDate =
         new(
             2026,
             8,
             20);

        DateOnly currentWorkDate =
            new(
                2026,
                8,
                21);

        var expectationResolver =
            new StubExpectationResolver();

        expectationResolver.Set(
            new ResolvedWorkExpectation(
                oldScheduleId,
                previousWorkDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                60,
                420,
                true,
                WorkExpectationSource.WeeklySchedule,
                Guid.NewGuid(),
                null));

        expectationResolver.Set(
            new ResolvedWorkExpectation(
                newScheduleId,
                currentWorkDate,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60,
                480,
                false,
                WorkExpectationSource.WeeklySchedule,
                Guid.NewGuid(),
                null));

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
                expectationResolver,
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
            CreateSingleScheduleContext();

        DateOnly workDate =
            new(
                2026,
                8,
                22);

        test.ExpectationResolver.Set(
            NonWorkingExpectation(
                test.ScheduleId,
                workDate,
                WorkExpectationSource.WeeklySchedule));

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

    [Fact]
    public async Task Holiday_ResolvesNonWorkingContextWithProvenance()
    {
        TestContext test =
            CreateSingleScheduleContext();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        Guid holidayId =
            Guid.NewGuid();

        test.ExpectationResolver.Set(
            new ResolvedWorkExpectation(
                test.ScheduleId,
                workDate,
                false,
                null,
                null,
                0,
                0,
                false,
                WorkExpectationSource.Holiday,
                holidayId,
                "Quốc khánh"));

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    9,
                    2,
                    1,
                    0));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.Context);

        Assert.False(
            result.Context!.IsWorkingDay);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            result.Context.ExpectationSource);

        Assert.Equal(
            holidayId,
            result.Context.ExpectationSourceId);

        Assert.Equal(
            "Quốc khánh",
            result.Context.ExpectationSourceName);
    }

    [Fact]
    public async Task OvernightDateOverride_AfterMidnight_UsesPreviousWorkDate()
    {
        TestContext test =
            CreateSingleScheduleContext();

        DateOnly previousDate =
            new(
                2026,
                9,
                2);

        DateOnly currentDate =
            previousDate.AddDays(
                1);

        Guid overrideId =
            Guid.NewGuid();

        test.ExpectationResolver.Set(
            new ResolvedWorkExpectation(
                test.ScheduleId,
                previousDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                30,
                450,
                true,
                WorkExpectationSource.DateOverride,
                overrideId,
                "Trực ngày lễ"));

        test.ExpectationResolver.Set(
            NonWorkingExpectation(
                test.ScheduleId,
                currentDate,
                WorkExpectationSource.Holiday,
                "Nghỉ lễ"));

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    9,
                    2,
                    18,
                    30));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            previousDate,
            result.Context!.WorkDate);

        Assert.Equal(
            WorkExpectationSource.DateOverride,
            result.Context.ExpectationSource);

        Assert.Equal(
            overrideId,
            result.Context.ExpectationSourceId);

        Assert.True(
            result.Context.ExpectedStartTime >
            result.Context.ExpectedEndTime);
    }

    [Fact]
    public async Task PreviousDateHoliday_DoesNotCreateOvernightCandidate()
    {
        TestContext test =
            CreateSingleScheduleContext();

        DateOnly previousDate =
            new(
                2026,
                9,
                2);

        DateOnly currentDate =
            previousDate.AddDays(
                1);

        test.ExpectationResolver.Set(
            NonWorkingExpectation(
                test.ScheduleId,
                previousDate,
                WorkExpectationSource.Holiday,
                "Quốc khánh"));

        Guid currentWeeklySourceId =
            Guid.NewGuid();

        test.ExpectationResolver.Set(
            new ResolvedWorkExpectation(
                test.ScheduleId,
                currentDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                60,
                420,
                true,
                WorkExpectationSource.WeeklySchedule,
                currentWeeklySourceId,
                null));

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    9,
                    2,
                    18,
                    30));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            currentDate,
            result.Context!.WorkDate);

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            result.Context.ExpectationSource);

        Assert.Equal(
            currentWeeklySourceId,
            result.Context.ExpectationSourceId);
    }

    [Fact]
    public async Task ExpectationResolverConsistencyError_ReturnsFailure()
    {
        TestContext test =
            CreateSingleScheduleContext();

        test.ExpectationResolver.ExceptionToThrow =
            new InvalidOperationException(
                "Nguồn kỳ vọng không nhất quán.");

        AttendancePunchContextResolutionResult result =
            await test.Resolver.ResolveAsync(
                test.EmployeeId,
                Utc(
                    2026,
                    9,
                    2,
                    1,
                    0));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nguồn kỳ vọng không nhất quán.",
            result.ErrorMessage);
    }

    private static TestContext CreateSingleScheduleContext()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

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

        var expectationResolver =
            new StubExpectationResolver();

        var resolver =
            new AttendancePunchContextResolver(
                new StubAssignmentRepository(
                    [assignment]),
                new StubScheduleRepository(
                    [schedule]),
                expectationResolver,
                new PlusSevenTimeZoneConverter());

        return new TestContext(
            employeeId,
            assignmentId,
            scheduleId,
            resolver,
            expectationResolver);
    }

    private sealed record TestContext(
    Guid EmployeeId,
    Guid AssignmentId,
    Guid ScheduleId,
    AttendancePunchContextResolver Resolver,
    StubExpectationResolver ExpectationResolver);

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

    private sealed class StubExpectationResolver
    : IWorkExpectationResolver
    {
        private readonly Dictionary<
            (Guid WorkScheduleId, DateOnly WorkDate),
            ResolvedWorkExpectation>
            _expectations =
                new();

        public Exception? ExceptionToThrow
        {
            get;
            set;
        }

        public int ResolveCallCount
        {
            get;
            private set;
        }

        public void Set(
            ResolvedWorkExpectation expectation)
        {
            _expectations[
                (
                    expectation.WorkScheduleId,
                    expectation.WorkDate
                )] =
                    expectation;
        }

        public Task<ResolvedWorkExpectation?> ResolveAsync(
            Guid workScheduleId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            ResolveCallCount++;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            _expectations.TryGetValue(
                (
                    workScheduleId,
                    workDate
                ),
                out ResolvedWorkExpectation? expectation);

            return Task.FromResult(
                expectation);
        }

        public Task<IReadOnlyDictionary<Guid, ResolvedWorkExpectation>>
            ResolveManyAsync(
                IReadOnlyCollection<Guid> workScheduleIds,
                DateOnly workDate,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, ResolvedWorkExpectation> result =
                workScheduleIds
                    .Distinct()
                    .Select(
                        workScheduleId =>
                        {
                            _expectations.TryGetValue(
                                (
                                    workScheduleId,
                                    workDate
                                ),
                                out ResolvedWorkExpectation? expectation);

                            return new
                            {
                                WorkScheduleId =
                                    workScheduleId,
                                Expectation =
                                    expectation
                            };
                        })
                    .Where(
                        item =>
                            item.Expectation is not null)
                    .ToDictionary(
                        item =>
                            item.WorkScheduleId,
                        item =>
                            item.Expectation!);

            return Task.FromResult(
                result);
        }
    }

    private static ResolvedWorkExpectation WorkingExpectation(
    Guid scheduleId,
    DateOnly workDate,
    TimeOnly startTime,
    TimeOnly endTime,
    WorkExpectationSource source =
        WorkExpectationSource.WeeklySchedule,
    Guid? sourceId = null,
    string? sourceName = null)
    {
        int startMinutes =
            startTime.Hour * 60
            + startTime.Minute;

        int endMinutes =
            endTime.Hour * 60
            + endTime.Minute;

        int shiftMinutes =
            endMinutes
            - startMinutes;

        if (shiftMinutes < 0)
        {
            shiftMinutes +=
                24 * 60;
        }

        return new ResolvedWorkExpectation(
            scheduleId,
            workDate,
            true,
            startTime,
            endTime,
            60,
            shiftMinutes - 60,
            endTime < startTime,
            source,
            sourceId ?? Guid.NewGuid(),
            sourceName);
    }

    private static ResolvedWorkExpectation NonWorkingExpectation(
        Guid scheduleId,
        DateOnly workDate,
        WorkExpectationSource source,
        string? sourceName = null)
    {
        return new ResolvedWorkExpectation(
            scheduleId,
            workDate,
            false,
            null,
            null,
            0,
            0,
            false,
            source,
            Guid.NewGuid(),
            sourceName);
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
