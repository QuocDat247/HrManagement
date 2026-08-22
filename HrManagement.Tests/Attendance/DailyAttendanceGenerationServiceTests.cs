using HrManagement.Application.Attendance.Generation;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class DailyAttendanceGenerationServiceTests
{
    [Fact]
    public async Task WorkingDay_CreatesExpectedSnapshot()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        Guid scheduleId =
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
                        "SE Asia Standard Time",
                        true,
                        new TimeOnly(
                            22,
                            0),
                        new TimeOnly(
                            6,
                            0),
                        30)
                ]
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        DateOnly workDate =
            new(
                2026,
                8,
                22);

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
            0,
            result.SkippedExistingCount);

        AttendanceRecord record =
            Assert.Single(
                persistence.AddedRecords);

        Assert.NotEqual(
            Guid.Empty,
            record.Id);

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

        Assert.Equal(
            "SE Asia Standard Time",
            record.TimeZoneId);

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
    }

    [Fact]
    public async Task NonWorkingDay_CreatesNonWorkingSnapshot()
    {
        Guid employeeId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    new DailyAttendanceGenerationCandidate(
                        employeeId,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "SE Asia Standard Time",
                        false,
                        null,
                        null,
                        0)
                ]
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        8,
                        23)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            1,
            result.CreatedCount);

        Assert.Equal(
            0,
            result.SkippedExistingCount);

        AttendanceRecord record =
            Assert.Single(
                persistence.AddedRecords);

        Assert.False(
            record.IsWorkingDay);

        Assert.Null(
            record.ExpectedStartTime);

        Assert.Null(
            record.ExpectedEndTime);

        Assert.Equal(
            0,
            record.ExpectedBreakMinutes);

        Assert.Equal(
            0,
            record.ExpectedPlannedMinutes);

        Assert.False(
            record.IsOvernight);
    }

    [Fact]
    public async Task ExistingRecord_IsSkipped()
    {
        Guid employeeId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateWorkingCandidate(
                        employeeId)
                ],

                ExistingEmployeeIds =
                [
                    employeeId
                ]
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        8,
                        22)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            0,
            result.CreatedCount);

        Assert.Equal(
            1,
            result.SkippedExistingCount);

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task MixedCandidates_CreatesOnlyMissingRecords()
    {
        Guid existingEmployeeId =
            Guid.NewGuid();

        Guid missingEmployeeId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    CreateWorkingCandidate(
                        existingEmployeeId),

                    CreateWorkingCandidate(
                        missingEmployeeId)
                ],

                ExistingEmployeeIds =
                [
                    existingEmployeeId
                ]
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        8,
                        22)));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            1,
            result.CreatedCount);

        Assert.Equal(
            1,
            result.SkippedExistingCount);

        AttendanceRecord created =
            Assert.Single(
                persistence.AddedRecords);

        Assert.Equal(
            missingEmployeeId,
            created.EmployeeId);

        Assert.DoesNotContain(
            persistence.AddedRecords,
            record =>
                record.EmployeeId ==
                existingEmployeeId);
    }

    [Fact]
    public async Task DuplicateEffectiveAssignments_Fails()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence
            {
                Candidates =
                [
                    new DailyAttendanceGenerationCandidate(
                        employeeId,
                        employmentPeriodId,
                        Guid.NewGuid(),
                        scheduleId,
                        "SE Asia Standard Time",
                        true,
                        new TimeOnly(
                            8,
                            0),
                        new TimeOnly(
                            17,
                            0),
                        60),

                    new DailyAttendanceGenerationCandidate(
                        employeeId,
                        employmentPeriodId,
                        Guid.NewGuid(),
                        scheduleId,
                        "SE Asia Standard Time",
                        true,
                        new TimeOnly(
                            8,
                            0),
                        new TimeOnly(
                            17,
                            0),
                        60)
                ]
            };

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        8,
                        22)));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Phát hiện nhiều phân lịch làm việc cùng hiệu lực cho một nhân viên.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            result.CreatedCount);

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

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        8,
                        22),
                    employeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy phân lịch làm việc phù hợp cho nhân viên tại ngày đã chọn.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            result.CreatedCount);

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task GenerateAsync_DefaultWorkDate_FailsWithoutPersistence()
    {
        var persistence =
            new TestPersistence();

        var service =
            new DailyAttendanceGenerationService(
                persistence);

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

        Assert.Empty(
            persistence.AddedRecords);
    }

    [Fact]
    public async Task GenerateAsync_EmptyEmployeeId_FailsWithoutPersistence()
    {
        var persistence =
            new TestPersistence();

        var service =
            new DailyAttendanceGenerationService(
                persistence);

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    new DateOnly(
                        2026,
                        8,
                        22),
                    Guid.Empty));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nhân viên không hợp lệ.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.GetCandidatesCallCount);

        Assert.Empty(
            persistence.AddedRecords);
    }



    private static DailyAttendanceGenerationCandidate
        CreateWorkingCandidate(
            Guid employeeId)
    {
        return new DailyAttendanceGenerationCandidate(
            employeeId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
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

        public IReadOnlyList<Guid>
            ExistingEmployeeIds
        {
            get;
            init;
        } =
            Array.Empty<Guid>();

        public List<AttendanceRecord>
            AddedRecords
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

            IReadOnlyList<
                DailyAttendanceGenerationCandidate>
                result =
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
