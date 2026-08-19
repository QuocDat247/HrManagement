using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Attendance;

public sealed class EmployeeWorkScheduleAssignmentServiceTests
{
    [Fact]
    public async Task AssignAsync_WhenNoAssignmentExists_CreatesInitialAssignment()
    {
        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreateOpenPeriod(
                employee.Id);

        WorkSchedule schedule =
            CreateSchedule();

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                schedule,
                [],
                persistence);

        DateOnly effectiveFrom =
            new(
                2026,
                8,
                1);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    schedule.Id,
                    effectiveFrom));

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            persistence.ClosedAssignment);

        Assert.NotNull(
            persistence.NewAssignment);

        Assert.Equal(
            employee.Id,
            persistence.NewAssignment!.EmployeeId);

        Assert.Equal(
            period.Id,
            persistence.NewAssignment.EmploymentPeriodId);

        Assert.Equal(
            schedule.Id,
            persistence.NewAssignment.WorkScheduleId);

        Assert.Equal(
            effectiveFrom,
            persistence.NewAssignment.EffectiveFrom);

        Assert.True(
            persistence.NewAssignment.IsOpen);
    }

    [Fact]
    public async Task AssignAsync_WhenChangingSchedule_ClosesOldDayBeforeNewStart()
    {
        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreateOpenPeriod(
                employee.Id);

        WorkSchedule oldSchedule =
            CreateSchedule(
                code: "OLD");

        WorkSchedule newSchedule =
            CreateSchedule(
                code: "NEW");

        var currentAssignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employee.Id,
                period.Id,
                oldSchedule.Id,
                new DateOnly(
                    2026,
                    8,
                    1));

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                newSchedule,
                [currentAssignment],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    newSchedule.Id,
                    new DateOnly(
                        2026,
                        9,
                        1)));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.ClosedAssignment);

        Assert.Equal(
            currentAssignment.Id,
            persistence.ClosedAssignment!.Id);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            persistence.ClosedAssignment.EffectiveTo);

        Assert.NotNull(
            persistence.NewAssignment);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                1),
            persistence.NewAssignment!.EffectiveFrom);

        Assert.Equal(
            newSchedule.Id,
            persistence.NewAssignment.WorkScheduleId);

        Assert.Equal(
            period.Id,
            persistence.NewAssignment.EmploymentPeriodId);
    }

    [Fact]
    public async Task AssignAsync_WhenScheduleDoesNotChange_Fails()
    {
        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreateOpenPeriod(
                employee.Id);

        WorkSchedule schedule =
            CreateSchedule();

        var currentAssignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employee.Id,
                period.Id,
                schedule.Id,
                new DateOnly(
                    2026,
                    8,
                    1));

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                schedule,
                [currentAssignment],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    schedule.Id,
                    new DateOnly(
                        2026,
                        9,
                        1)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.NewAssignment);
    }

    [Fact]
    public async Task AssignAsync_WhenNewDateIsNotAfterTailStart_Fails()
    {
        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreateOpenPeriod(
                employee.Id);

        WorkSchedule oldSchedule =
            CreateSchedule(
                code: "OLD");

        WorkSchedule newSchedule =
            CreateSchedule(
                code: "NEW");

        var currentAssignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employee.Id,
                period.Id,
                oldSchedule.Id,
                new DateOnly(
                    2026,
                    8,
                    1));

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                newSchedule,
                [currentAssignment],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    newSchedule.Id,
                    new DateOnly(
                        2026,
                        8,
                        1)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.NewAssignment);
    }

    [Fact]
    public async Task AssignAsync_WhenScheduleIsInactive_Fails()
    {
        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreateOpenPeriod(
                employee.Id);

        WorkSchedule schedule =
            CreateSchedule(
                isActive: false);

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                schedule,
                [],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    schedule.Id,
                    new DateOnly(
                        2026,
                        8,
                        1)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.NewAssignment);
    }

    [Fact]
    public async Task AssignAsync_WhenEmployeeIsInactive_Fails()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Inactive);

        EmploymentPeriod period =
            CreateOpenPeriod(
                employee.Id);

        WorkSchedule schedule =
            CreateSchedule();

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                schedule,
                [],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    schedule.Id,
                    new DateOnly(
                        2026,
                        8,
                        1)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.NewAssignment);
    }

    [Fact]
    public async Task AssignAsync_WhenNoOpenEmploymentPeriod_Fails()
    {
        Employee employee =
            CreateEmployee();

        var closedPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employee.Id,
                new DateOnly(
                    2025,
                    1,
                    1),
                new DateOnly(
                    2025,
                    12,
                    31));

        WorkSchedule schedule =
            CreateSchedule();

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [closedPeriod]),
                schedule,
                [],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    schedule.Id,
                    new DateOnly(
                        2026,
                        8,
                        1)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.NewAssignment);
    }

    [Fact]
    public async Task AssignAsync_WhenInitialDateIsBeforeEmploymentPeriod_Fails()
    {
        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            new(
                Guid.NewGuid(),
                employee.Id,
                new DateOnly(
                    2026,
                    8,
                    10));

        WorkSchedule schedule =
            CreateSchedule();

        var persistence =
            new CapturingPersistence();

        var service =
            CreateService(
                employee,
                new EmploymentHistory(
                    employee.Id,
                    [period]),
                schedule,
                [],
                persistence);

        AssignEmployeeWorkScheduleResult result =
            await service.AssignAsync(
                new AssignEmployeeWorkScheduleRequest(
                    employee.Id,
                    schedule.Id,
                    new DateOnly(
                        2026,
                        8,
                        9)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.NewAssignment);
    }

    private static EmployeeWorkScheduleAssignmentService
        CreateService(
            Employee employee,
            EmploymentHistory employmentHistory,
            WorkSchedule schedule,
            IReadOnlyList<EmployeeWorkScheduleAssignment>
                assignments,
            CapturingPersistence persistence)
    {
        return new EmployeeWorkScheduleAssignmentService(
            new StubEmployeeRepository(
                employee),
            new StubEmploymentHistoryRepository(
                employmentHistory),
            new StubWorkScheduleRepository(
                schedule),
            new StubAssignmentRepository(
                assignments),
            persistence);
    }

    private static Employee CreateEmployee(
        EmployeeStatus status = EmployeeStatus.Active)
    {
        Guid employeeId =
            Guid.NewGuid();

        return new Employee(
            employeeId,
            $"EMP-{employeeId:N}"[..20],
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(
                2025,
                1,
                1),
            "Phòng kiểm thử",
            "Chuyên viên kiểm thử",
            status);
    }

    private static EmploymentPeriod CreateOpenPeriod(
        Guid employeeId)
    {
        return new EmploymentPeriod(
            Guid.NewGuid(),
            employeeId,
            new DateOnly(
                2026,
                1,
                1));
    }

    private static WorkSchedule CreateSchedule(
        string code = "OFFICE",
        bool isActive = true)
    {
        return new WorkSchedule(
            Guid.NewGuid(),
            code,
            $"Lịch {code}",
            "SE Asia Standard Time",
            isActive);
    }

    private sealed class StubEmployeeRepository
        : IEmployeeRepository
    {
        private readonly Employee
            _employee;

        public StubEmployeeRepository(
            Employee employee)
        {
            _employee =
                employee;
        }

        public Task<Employee?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            Employee? result =
                id == _employee.Id
                    ? _employee
                    : null;

            return Task.FromResult(
                result);
        }

        public Task<IReadOnlyList<Employee>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Employee> result =
                [_employee];

            return Task.FromResult(
                result);
        }

        public Task<Employee?> GetByEmployeeCodeAsync(
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            Employee? result =
                employeeCode ==
                _employee.EmployeeCode
                    ? _employee
                    : null;

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubEmploymentHistoryRepository
        : IEmploymentHistoryRepository
    {
        private readonly EmploymentHistory
            _history;

        public StubEmploymentHistoryRepository(
            EmploymentHistory history)
        {
            _history =
                history;
        }

        public Task<EmploymentHistory> GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _history);
        }

        public Task AddPeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdatePeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubWorkScheduleRepository
        : IWorkScheduleRepository
    {
        private readonly WorkSchedule
            _schedule;

        public StubWorkScheduleRepository(
            WorkSchedule schedule)
        {
            _schedule =
                schedule;
        }

        public Task<WorkSchedule?> GetByIdAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            WorkSchedule? result =
                workScheduleId ==
                _schedule.Id
                    ? _schedule
                    : null;

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubAssignmentRepository
        : IEmployeeWorkScheduleAssignmentRepository
    {
        private readonly
            IReadOnlyList<EmployeeWorkScheduleAssignment>
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
            return Task.FromResult(
                _assignments);
        }
    }

    private sealed class CapturingPersistence
        : IEmployeeWorkScheduleAssignmentPersistence
    {
        public EmployeeWorkScheduleAssignment?
            ClosedAssignment
        {
            get;
            private set;
        }

        public EmployeeWorkScheduleAssignment?
            NewAssignment
        {
            get;
            private set;
        }

        public Task ApplyAsync(
            EmployeeWorkScheduleAssignment? closedAssignment,
            EmployeeWorkScheduleAssignment newAssignment,
            CancellationToken cancellationToken = default)
        {
            ClosedAssignment =
                closedAssignment;

            NewAssignment =
                newAssignment;

            return Task.CompletedTask;
        }
    }
}
