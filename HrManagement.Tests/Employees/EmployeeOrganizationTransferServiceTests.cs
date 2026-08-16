using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeOrganizationTransferServiceTests
{
    [Fact]
    public async Task TransferAsync_WhenValid_TransfersEmployeeAndAssignment()
    {
        TestScenario scenario =
            CreateScenario();

        DateOnly effectiveDate =
            DateOnly.FromDateTime(
                DateTime.Today);

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                effectiveDate);

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.Equal(
            1,
            scenario.Persistence.CallCount);

        Employee persistedEmployee =
            Assert.IsType<Employee>(
                scenario.Persistence.Employee);

        EmployeeOrganizationAssignment closedAssignment =
            Assert.IsType<EmployeeOrganizationAssignment>(
                scenario.Persistence.ClosedAssignment);

        EmployeeOrganizationAssignment newAssignment =
            Assert.IsType<EmployeeOrganizationAssignment>(
                scenario.Persistence.NewAssignment);

        Assert.Equal(
            scenario.TargetDepartment.Id,
            persistedEmployee.DepartmentId);

        Assert.Equal(
            scenario.TargetPosition.Id,
            persistedEmployee.PositionId);

        Assert.Equal(
            scenario.TargetDepartment.Name,
            persistedEmployee.Department);

        Assert.Equal(
            scenario.TargetPosition.Name,
            persistedEmployee.Position);

        Assert.Equal(
            scenario.Employee.Status,
            persistedEmployee.Status);

        Assert.Equal(
            scenario.CurrentAssignment.Id,
            closedAssignment.Id);

        Assert.Equal(
            effectiveDate.AddDays(-1),
            closedAssignment.EndDate);

        Assert.False(
            closedAssignment.IsOpen);

        Assert.Equal(
            effectiveDate,
            newAssignment.StartDate);

        Assert.Null(
            newAssignment.EndDate);

        Assert.True(
            newAssignment.IsOpen);

        Assert.Equal(
            scenario.CurrentPeriod.Id,
            newAssignment.EmploymentPeriodId);

        Assert.Equal(
            scenario.TargetDepartment.Id,
            newAssignment.DepartmentId);

        Assert.Equal(
            scenario.TargetDepartment.Code,
            newAssignment.DepartmentCode);

        Assert.Equal(
            scenario.TargetDepartment.Name,
            newAssignment.DepartmentName);

        Assert.Equal(
            scenario.TargetPosition.Id,
            newAssignment.PositionId);

        Assert.Equal(
            scenario.TargetPosition.Code,
            newAssignment.PositionCode);

        Assert.Equal(
            scenario.TargetPosition.Name,
            newAssignment.PositionName);

        Assert.False(
            newAssignment.IsBaseline);
    }

    [Fact]
    public async Task TransferAsync_WhenEmployeeIsInactive_ReturnsFailure()
    {
        TestScenario scenario =
            CreateScenario(
                status:
                    EmployeeStatus.Inactive);

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                DateOnly.FromDateTime(
                    DateTime.Today));

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Chỉ có thể điều chuyển nhân viên "
            + "đang làm việc hoặc nghỉ phép.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            scenario.Persistence.CallCount);

        Assert.Null(
            scenario.Persistence.Employee);
    }

    [Fact]
    public async Task TransferAsync_WhenEffectiveDateIsInFuture_ReturnsFailure()
    {
        TestScenario scenario =
            CreateScenario();

        DateOnly tomorrow =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(1);

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                tomorrow);

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày điều chuyển không thể ở tương lai.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            scenario.Persistence.CallCount);
    }

    [Fact]
    public async Task TransferAsync_WhenTargetDepartmentIsInactive_ReturnsFailure()
    {
        TestScenario scenario =
            CreateScenario(
                targetDepartmentIsActive: false);

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                DateOnly.FromDateTime(
                    DateTime.Today));

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Phòng ban đã ngừng sử dụng.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            scenario.Persistence.CallCount);

        Assert.True(
            scenario.CurrentAssignment.IsOpen);
    }

    [Fact]
    public async Task TransferAsync_WhenOrganizationDoesNotChange_ReturnsFailure()
    {
        TestScenario scenario =
            CreateScenario(
                targetSameAsSource: true);

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                DateOnly.FromDateTime(
                    DateTime.Today));

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Điều chuyển phải thay đổi phòng ban, "
            + "chức danh hoặc cả hai.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            scenario.Persistence.CallCount);

        Assert.True(
            scenario.CurrentAssignment.IsOpen);

        Assert.Null(
            scenario.CurrentAssignment.EndDate);
    }

    [Fact]
    public async Task TransferAsync_WhenCurrentAssignmentBelongsToDifferentEmploymentPeriod_ReturnsFailure()
    {
        TestScenario scenario =
            CreateScenario(
                assignmentUsesDifferentPeriod: true);

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                DateOnly.FromDateTime(
                    DateTime.Today));

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Phân công hiện tại không khớp với "
            + "giai đoạn làm việc đang mở.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            scenario.Persistence.CallCount);

        Assert.True(
            scenario.CurrentAssignment.IsOpen);
    }

    [Fact]
    public async Task TransferAsync_WhenEffectiveDateEqualsCurrentAssignmentStartDate_ReturnsFailure()
    {
        TestScenario scenario =
            CreateScenario();

        var request =
            new TransferEmployeeOrganizationRequest(
                scenario.Employee.Id,
                scenario.TargetDepartment.Id,
                scenario.TargetPosition.Id,
                scenario.CurrentAssignment.StartDate);

        TransferEmployeeOrganizationResult result =
            await scenario.Service.TransferAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.StartsWith(
            "Ngày điều chuyển phải sau ngày bắt đầu "
            + "của phân công hiện tại.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            scenario.Persistence.CallCount);

        // Domain validation phải xảy ra
        // trước khi assignment hiện tại bị đóng.
        Assert.True(
            scenario.CurrentAssignment.IsOpen);

        Assert.Null(
            scenario.CurrentAssignment.EndDate);
    }

    private static TestScenario CreateScenario(
        EmployeeStatus status = EmployeeStatus.Active,
        bool targetDepartmentIsActive = true,
        bool targetSameAsSource = false,
        bool assignmentUsesDifferentPeriod = false)
    {
        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Today);

        DateOnly startDate =
            today.AddDays(-30);

        var sourceDepartment =
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm");

        var sourcePosition =
            new Position(
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm");

        Department targetDepartment =
            targetSameAsSource
                ? sourceDepartment
                : new Department(
                    Guid.NewGuid(),
                    "RD",
                    "Nghiên cứu và phát triển",
                    targetDepartmentIsActive);

        Position targetPosition =
            targetSameAsSource
                ? sourcePosition
                : new Position(
                    Guid.NewGuid(),
                    "LEAD",
                    "Trưởng nhóm kỹ thuật");

        DateOnly? terminationDate =
            status == EmployeeStatus.Inactive
                ? today.AddDays(-1)
                : null;

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-TRANSFER-SVC-001",
                "Nhân viên điều chuyển",
                "transfer@example.com",
                "0901000000",
                new DateOnly(1995, 1, 1),
                startDate,
                sourceDepartment.Name,
                sourcePosition.Name,
                status,
                terminationDate,
                departmentId:
                    sourceDepartment.Id,
                positionId:
                    sourcePosition.Id);

        var currentPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employee.Id,
                startDate);

        Guid assignmentPeriodId =
            assignmentUsesDifferentPeriod
                ? Guid.NewGuid()
                : currentPeriod.Id;

        var currentAssignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employee.Id,
                assignmentPeriodId,
                sourceDepartment.Id,
                sourceDepartment.Code,
                sourceDepartment.Name,
                sourcePosition.Id,
                sourcePosition.Code,
                sourcePosition.Name,
                startDate);

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var employmentHistoryRepository =
            new StubEmploymentHistoryRepository
            {
                History =
                    new EmploymentHistory(
                        employee.Id,
                        [currentPeriod])
            };

        var organizationHistoryRepository =
            new StubEmployeeOrganizationHistoryRepository
            {
                History =
                    new EmployeeOrganizationHistory(
                        employee.Id,
                        [currentAssignment])
            };

        var departmentRepository =
            new StubDepartmentRepository();

        departmentRepository.Departments.Add(
            sourceDepartment);

        if (!targetSameAsSource)
        {
            departmentRepository.Departments.Add(
                targetDepartment);
        }

        var positionRepository =
            new StubPositionRepository();

        positionRepository.Positions.Add(
            sourcePosition);

        if (!targetSameAsSource)
        {
            positionRepository.Positions.Add(
                targetPosition);
        }

        var persistence =
            new StubTransferPersistence();

        var service =
            new EmployeeOrganizationTransferService(
                employeeRepository,
                employmentHistoryRepository,
                organizationHistoryRepository,
                departmentRepository,
                positionRepository,
                persistence);

        return new TestScenario(
            service,
            persistence,
            employee,
            currentPeriod,
            currentAssignment,
            sourceDepartment,
            targetDepartment,
            sourcePosition,
            targetPosition);
    }

    private sealed record TestScenario(
        EmployeeOrganizationTransferService Service,
        StubTransferPersistence Persistence,
        Employee Employee,
        EmploymentPeriod CurrentPeriod,
        EmployeeOrganizationAssignment CurrentAssignment,
        Department SourceDepartment,
        Department TargetDepartment,
        Position SourcePosition,
        Position TargetPosition);

    private sealed class StubTransferPersistence
        : IEmployeeOrganizationTransferPersistence
    {
        public int CallCount
        {
            get;
            private set;
        }

        public Employee? Employee
        {
            get;
            private set;
        }

        public EmployeeOrganizationAssignment?
            ClosedAssignment
        {
            get;
            private set;
        }

        public EmployeeOrganizationAssignment?
            NewAssignment
        {
            get;
            private set;
        }

        public Task TransferEmployeeOrganizationAsync(
            Employee employee,
            EmployeeOrganizationAssignment closedAssignment,
            EmployeeOrganizationAssignment newAssignment,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            Employee =
                employee;

            ClosedAssignment =
                closedAssignment;

            NewAssignment =
                newAssignment;

            return Task.CompletedTask;
        }
    }

    private sealed class
        StubEmployeeOrganizationHistoryRepository
        : IEmployeeOrganizationHistoryRepository
    {
        public EmployeeOrganizationHistory? History
        {
            get;
            set;
        }

        public Task<EmployeeOrganizationHistory>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                History
                ?? new EmployeeOrganizationHistory(
                    employeeId,
                    []));
        }

        public Task AddAssignmentAsync(
            EmployeeOrganizationAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAssignmentAsync(
            EmployeeOrganizationAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubEmploymentHistoryRepository
        : IEmploymentHistoryRepository
    {
        public EmploymentHistory? History
        {
            get;
            set;
        }

        public Task<EmploymentHistory>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                History
                ?? new EmploymentHistory(
                    employeeId,
                    []));
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

    private sealed class StubEmployeeRepository
        : IEmployeeRepository
    {
        public List<Employee> Employees
        {
            get;
        }

        public StubEmployeeRepository(
            params Employee[] employees)
        {
            Employees =
                employees.ToList();
        }

        public Task<IReadOnlyList<Employee>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<Employee>>(
                    Employees);
        }

        public Task<Employee?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Employees.SingleOrDefault(
                    employee =>
                        employee.Id == id));
        }

        public Task<Employee?>
            GetByEmployeeCodeAsync(
                string employeeCode,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Employees.SingleOrDefault(
                    employee =>
                        string.Equals(
                            employee.EmployeeCode,
                            employeeCode,
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            Employees.Add(
                employee);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            int index =
                Employees.FindIndex(
                    existing =>
                        existing.Id ==
                        employee.Id);

            if (index >= 0)
            {
                Employees[index] =
                    employee;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StubDepartmentRepository
        : IDepartmentRepository
    {
        public List<Department> Departments
        {
            get;
        } = [];

        public Task<IReadOnlyList<Department>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<Department>>(
                    Departments);
        }

        public Task<Department?> GetByIdAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Departments.SingleOrDefault(
                    department =>
                        department.Id ==
                        departmentId));
        }

        public Task<Department?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Departments.SingleOrDefault(
                    department =>
                        string.Equals(
                            department.Code,
                            code,
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            Departments.Add(
                department);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            int index =
                Departments.FindIndex(
                    existing =>
                        existing.Id ==
                        department.Id);

            if (index >= 0)
            {
                Departments[index] =
                    department;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StubPositionRepository
        : IPositionRepository
    {
        public List<Position> Positions
        {
            get;
        } = [];

        public Task<IReadOnlyList<Position>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<Position>>(
                    Positions);
        }

        public Task<Position?> GetByIdAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Positions.SingleOrDefault(
                    position =>
                        position.Id ==
                        positionId));
        }

        public Task<Position?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Positions.SingleOrDefault(
                    position =>
                        string.Equals(
                            position.Code,
                            code,
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(
            Position position,
            CancellationToken cancellationToken = default)
        {
            Positions.Add(
                position);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Position position,
            CancellationToken cancellationToken = default)
        {
            int index =
                Positions.FindIndex(
                    existing =>
                        existing.Id ==
                        position.Id);

            if (index >= 0)
            {
                Positions[index] =
                    position;
            }

            return Task.CompletedTask;
        }
    }
}
