using HrManagement.Application.Attendance.Expectations;
using HrManagement.Application.Attendance.Generation;
using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class DailyAttendanceGenerationServiceTests
{
    [Fact]
    public async Task WorkingDay_CreatesResolvedExpectationSnapshot()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        Guid sourceId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    new DailyAttendanceGenerationCandidate(
                        employeeId,
                        employmentPeriodId,
                        assignmentId,
                        scheduleId,
                        "SE Asia Standard Time")
                ]
            };

        var resolver =
            new TestExpectationResolver();

        resolver.Expectations[scheduleId] =
            new ResolvedWorkExpectation(
                scheduleId,
                new DateOnly(
                    2026,
                    9,
                    2),
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
                sourceId,
                "Trực ngày lễ");

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            1,
            result.CreatedCount);

        AttendanceRecord record =
            Assert.Single(
                persistence.AddedRecords);

        Assert.Equal(
            employeeId,
            record.EmployeeId);

        Assert.Equal(
            employmentPeriodId,
            record.EmploymentPeriodId);

        Assert.Equal(
            assignmentId,
            record.WorkScheduleAssignmentId);

        Assert.Equal(
            scheduleId,
            record.WorkScheduleId);

        Assert.Equal(
            workDate,
            record.WorkDate);

        Assert.True(
            record.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                22,
                0),
            record.ExpectedStartTime);

        Assert.Equal(
            new TimeOnly(
                6,
                0),
            record.ExpectedEndTime);

        Assert.Equal(
            30,
            record.ExpectedBreakMinutes);

        Assert.Equal(
            450,
            record.ExpectedPlannedMinutes);

        Assert.True(
            record.IsOvernight);

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
    public async Task Holiday_CreatesNonWorkingSnapshot()
    {
        Guid scheduleId =
            Guid.NewGuid();

        Guid holidayId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        Guid.NewGuid(),
                        scheduleId)
                ]
            };

        var resolver =
            new TestExpectationResolver();

        resolver.Expectations[scheduleId] =
            new ResolvedWorkExpectation(
                scheduleId,
                workDate,
                false,
                null,
                null,
                0,
                0,
                false,
                WorkExpectationSource.Holiday,
                holidayId,
                "Quốc khánh");

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate));

        Assert.True(
            result.IsSuccessful);

        AttendanceRecord record =
            Assert.Single(
                persistence.AddedRecords);

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
    public async Task ExistingRecord_IsSkippedWithoutResolvingAgain()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        employeeId,
                        scheduleId)
                ],

                ExistingEmployeeIds =
                [
                    employeeId
                ]
            };

        var resolver =
            new TestExpectationResolver();

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        9,
                        2)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            0,
            result.CreatedCount);

        Assert.Equal(
            1,
            result.SkippedExistingCount);

        Assert.Equal(
            0,
            resolver.ResolveManyCallCount);

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task MixedCandidates_ResolvesOnlyMissingSchedules()
    {
        Guid existingEmployeeId =
            Guid.NewGuid();

        Guid missingEmployeeId =
            Guid.NewGuid();

        Guid existingScheduleId =
            Guid.NewGuid();

        Guid missingScheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        existingEmployeeId,
                        existingScheduleId),

                    CreateCandidate(
                        missingEmployeeId,
                        missingScheduleId)
                ],

                ExistingEmployeeIds =
                [
                    existingEmployeeId
                ]
            };

        var resolver =
            new TestExpectationResolver();

        resolver.Expectations[missingScheduleId] =
            CreateWeeklyExpectation(
                missingScheduleId,
                workDate);

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            1,
            result.CreatedCount);

        Assert.Equal(
            1,
            result.SkippedExistingCount);

        Guid resolvedScheduleId =
            Assert.Single(
                resolver.LastWorkScheduleIds);

        Assert.Equal(
            missingScheduleId,
            resolvedScheduleId);

        AttendanceRecord created =
            Assert.Single(
                persistence.AddedRecords);

        Assert.Equal(
            missingEmployeeId,
            created.EmployeeId);
    }

    [Fact]
    public async Task DuplicateEffectiveAssignments_FailsBeforeResolution()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        employeeId,
                        scheduleId),

                    CreateCandidate(
                        employeeId,
                        scheduleId)
                ]
            };

        var resolver =
            new TestExpectationResolver();

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        9,
                        2)));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Phát hiện nhiều phân lịch làm việc cùng hiệu lực cho một nhân viên.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            resolver.ResolveManyCallCount);

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task SpecificEmployeeWithoutAssignment_Fails()
    {
        Guid employeeId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence();

        var resolver =
            new TestExpectationResolver();

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        9,
                        2),
                    employeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy phân lịch làm việc phù hợp cho nhân viên tại ngày đã chọn.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            resolver.ResolveManyCallCount);
    }

    [Fact]
    public async Task GenerateAsync_DefaultWorkDate_FailsWithoutPersistence()
    {
        var persistence =
            new TestPersistence();

        var resolver =
            new TestExpectationResolver();

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    default));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày sinh dữ liệu chấm công không hợp lệ.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.GetCandidatesCallCount);

        Assert.Equal(
            0,
            resolver.ResolveManyCallCount);
    }

    [Fact]
    public async Task GenerateAsync_EmptyEmployeeId_FailsWithoutPersistence()
    {
        var persistence =
            new TestPersistence();

        var resolver =
            new TestExpectationResolver();

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        9,
                        2),
                    Guid.Empty));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nhân viên không hợp lệ.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.GetCandidatesCallCount);

        Assert.Equal(
            0,
            resolver.ResolveManyCallCount);
    }

    [Fact]
    public async Task MissingExpectation_FailsWithoutPartialPersistence()
    {
        Guid scheduleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        Guid.NewGuid(),
                        scheduleId)
                ]
            };

        var resolver =
            new TestExpectationResolver();

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        9,
                        2)));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Lịch làm việc chưa có cấu hình kỳ vọng cho ngày sinh dữ liệu chấm công.",
            result.ErrorMessage);

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task ResolverConsistencyError_FailsWithoutPersistence()
    {
        Guid scheduleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        Guid.NewGuid(),
                        scheduleId)
                ]
            };

        var resolver =
            new TestExpectationResolver
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "Nguồn kỳ vọng không nhất quán.")
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        9,
                        2)));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nguồn kỳ vọng không nhất quán.",
            result.ErrorMessage);

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task MultipleEmployeesUsingSameSchedule_ResolveScheduleOnce()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        Guid.NewGuid(),
                        scheduleId),

                    CreateCandidate(
                        Guid.NewGuid(),
                        scheduleId)
                ]
            };

        var resolver =
            new TestExpectationResolver();

        resolver.Expectations[scheduleId] =
            CreateWeeklyExpectation(
                scheduleId,
                workDate);

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            2,
            result.CreatedCount);

        Assert.Equal(
            1,
            resolver.ResolveManyCallCount);

        Assert.Single(
            resolver.LastWorkScheduleIds);

        Assert.Equal(
            2,
            persistence.AddedRecords.Count);
    }

    [Fact]
    public async Task WeeklyExpectation_CapturesWeeklySourceId()
    {
        Guid scheduleId =
            Guid.NewGuid();

        Guid weeklyDayId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateCandidate(
                        Guid.NewGuid(),
                        scheduleId)
                ]
            };

        var resolver =
            new TestExpectationResolver();

        resolver.Expectations[scheduleId] =
            new ResolvedWorkExpectation(
                scheduleId,
                workDate,
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
                weeklyDayId,
                null);

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate));

        Assert.True(
            result.IsSuccessful);

        AttendanceRecord record =
            Assert.Single(
                persistence.AddedRecords);

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            record.ExpectationSource);

        Assert.Equal(
            weeklyDayId,
            record.ExpectationSourceId);
    }

    [Fact]
    public async Task GenerateAsync_WhenPeriodIsClosed_FailsBeforePersistence()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                24);

        var persistence =
            new TestPersistence();

        var resolver =
            new TestExpectationResolver();

        var periodLockPolicy =
            new StubAttendancePeriodLockPolicy
            {
                IsLocked = true
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence,
                resolver,
                periodLockPolicy);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công của ngày đã chọn đã được đóng. Không thể sinh dữ liệu chấm công.",
            result.ErrorMessage);

        Assert.Equal(
            1,
            periodLockPolicy.CallCount);

        Assert.Equal(
            workDate,
            periodLockPolicy.LastWorkDate);

        Assert.Equal(
            0,
            persistence.GetCandidatesCallCount);

        Assert.Equal(
            0,
            resolver.ResolveManyCallCount);

        Assert.Empty(
            persistence.AddedRecords);
    }

    private static DailyAttendanceGenerationCandidate CreateCandidate(
        Guid employeeId,
        Guid workScheduleId)
    {
        return new DailyAttendanceGenerationCandidate(
            employeeId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            workScheduleId,
            "SE Asia Standard Time");
    }

    private static ResolvedWorkExpectation CreateWeeklyExpectation(
        Guid workScheduleId,
        DateOnly workDate)
    {
        return new ResolvedWorkExpectation(
            workScheduleId,
            workDate,
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
            null);
    }

    private sealed class TestExpectationResolver
        : IWorkExpectationResolver
    {
        public Dictionary<Guid, ResolvedWorkExpectation>
            Expectations
        {
            get;
        } =
            new();

        public Exception? ExceptionToThrow
        {
            get;
            init;
        }

        public int ResolveManyCallCount
        {
            get;
            private set;
        }

        public IReadOnlyCollection<Guid> LastWorkScheduleIds
        {
            get;
            private set;
        } =
            Array.Empty<Guid>();

        public Task<ResolvedWorkExpectation?> ResolveAsync(
            Guid workScheduleId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            Expectations.TryGetValue(
                workScheduleId,
                out ResolvedWorkExpectation? result);

            return Task.FromResult(
                result);
        }

        public Task<IReadOnlyDictionary<Guid, ResolvedWorkExpectation>>
            ResolveManyAsync(
                IReadOnlyCollection<Guid> workScheduleIds,
                DateOnly workDate,
                CancellationToken cancellationToken = default)
        {
            ResolveManyCallCount++;

            LastWorkScheduleIds =
                workScheduleIds
                    .ToArray();

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            IReadOnlyDictionary<Guid, ResolvedWorkExpectation> result =
                Expectations
                    .Where(
                        pair =>
                            workScheduleIds.Contains(
                                pair.Key))
                    .ToDictionary();

            return Task.FromResult(
                result);
        }
    }

    private sealed class TestPersistence
        : IDailyAttendanceGenerationPersistence
    {
        public int GetCandidatesCallCount
        {
            get;
            private set;
        }

        public IReadOnlyList<DailyAttendanceGenerationCandidate>
            Candidates
        {
            get;
            init;
        } =
            Array.Empty<DailyAttendanceGenerationCandidate>();

        public IReadOnlyList<Guid> ExistingEmployeeIds
        {
            get;
            init;
        } =
            Array.Empty<Guid>();

        public List<AttendanceRecord> AddedRecords
        {
            get;
        } =
            new();

        public Task<IReadOnlyList<
            DailyAttendanceGenerationCandidate>>
            GetCandidatesAsync(
                DateOnly workDate,
                Guid? employeeId = null,
                CancellationToken cancellationToken = default)
        {
            GetCandidatesCallCount++;

            IReadOnlyList<DailyAttendanceGenerationCandidate> result =
                employeeId.HasValue
                    ? Candidates
                        .Where(
                            candidate =>
                                candidate.EmployeeId ==
                                employeeId.Value)
                        .ToArray()
                    : Candidates;

            return Task.FromResult(
                result);
        }

        public Task<IReadOnlyList<Guid>>
            GetExistingEmployeeIdsAsync(
                DateOnly workDate,
                IReadOnlyCollection<Guid> employeeIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Guid> result =
                ExistingEmployeeIds
                    .Where(
                        employeeIds.Contains)
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task AddRangeAsync(
            IReadOnlyList<AttendanceRecord> records,
            CancellationToken cancellationToken = default)
        {
            AddedRecords.AddRange(
                records);

            return Task.CompletedTask;
        }
    }
}
