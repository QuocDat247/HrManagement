using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Application.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IDepartmentRepository
    _departmentRepository;

    private readonly IPositionRepository
        _positionRepository;

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmploymentHistoryRepository
    _employmentHistoryRepository;

    private readonly IEmploymentLifecyclePersistence
        _employmentLifecyclePersistence;

    public EmployeeService(
    IEmployeeRepository employeeRepository,
    IEmploymentHistoryRepository employmentHistoryRepository,
    IEmploymentLifecyclePersistence employmentLifecyclePersistence,
    IDepartmentRepository departmentRepository,
    IPositionRepository positionRepository)
    {
        _employeeRepository =
            employeeRepository;

        _employmentHistoryRepository =
            employmentHistoryRepository;

        _employmentLifecyclePersistence =
            employmentLifecyclePersistence;

        _departmentRepository =
            departmentRepository;

        _positionRepository =
            positionRepository;
    }

    public async Task<RehireEmployeeResult>
    RehireEmployeeAsync(
        Guid employeeId,
        DateOnly rehireDate,
        EmployeeStatus rehireStatus,
        CancellationToken cancellationToken = default)
    {
        Employee? existingEmployee =
            await _employeeRepository.GetByIdAsync(
                employeeId,
                cancellationToken);

        if (existingEmployee is null)
        {
            return new RehireEmployeeResult(
                false,
                "Không tìm thấy nhân viên.");
        }

        if (existingEmployee.Status
            != EmployeeStatus.Inactive)
        {
            return new RehireEmployeeResult(
                false,
                "Chỉ có thể tái tuyển dụng nhân viên đã ngừng hoạt động.");
        }

        if (!existingEmployee.TerminationDate.HasValue)
        {
            return new RehireEmployeeResult(
                false,
                "Không thể tái tuyển dụng vì hồ sơ chưa có ngày nghỉ việc.");
        }

        if (rehireStatus is not EmployeeStatus.Active
            and not EmployeeStatus.OnLeave)
        {
            return new RehireEmployeeResult(
                false,
                "Trạng thái tái tuyển dụng phải là Đang làm việc hoặc Nghỉ phép.");
        }

        if (rehireDate == default)
        {
            return new RehireEmployeeResult(
                false,
                "Ngày tái tuyển dụng không hợp lệ.");
        }

        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Today);

        if (rehireDate > today)
        {
            return new RehireEmployeeResult(
                false,
                "Ngày tái tuyển dụng không thể ở tương lai.");
        }

        EmploymentHistory employmentHistory =
            await _employmentHistoryRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        EmploymentPeriod? latestPeriod =
            employmentHistory.LatestPeriod;

        if (latestPeriod is null)
        {
            return new RehireEmployeeResult(
                false,
                "Không tìm thấy lịch sử làm việc của nhân viên.");
        }

        if (employmentHistory.CurrentPeriod is not null)
        {
            return new RehireEmployeeResult(
                false,
                "Nhân viên đã có giai đoạn làm việc đang mở.");
        }

        if (latestPeriod.EndDate
            != existingEmployee.TerminationDate)
        {
            return new RehireEmployeeResult(
                false,
                "Ngày kết thúc của lịch sử làm việc không khớp.");
        }

        EmploymentPeriod newPeriod;

        try
        {
            newPeriod =
                employmentHistory.StartNewPeriod(
                    Guid.NewGuid(),
                    rehireDate);
        }
        catch (ArgumentException exception)
        {
            return new RehireEmployeeResult(
                false,
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return new RehireEmployeeResult(
                false,
                exception.Message);
        }

        var rehiredEmployee =
            new Employee(
                existingEmployee.Id,
                existingEmployee.EmployeeCode,
                existingEmployee.FullName,
                existingEmployee.Email,
                existingEmployee.PhoneNumber,
                existingEmployee.DateOfBirth,
                // CỐ Ý giữ ngày tuyển ban đầu.
                existingEmployee.HireDate,
                existingEmployee.Department,
                existingEmployee.Position,
                rehireStatus,
                terminationDate: null,
                departmentId: existingEmployee.DepartmentId,
                positionId: existingEmployee.PositionId);

        await _employmentLifecyclePersistence
            .UpdateEmployeeWithNewPeriodAsync(
                rehiredEmployee,
                newPeriod,
                cancellationToken);

        return new RehireEmployeeResult(
            true,
            null);
    }

    public async Task<CancelEmployeeDeactivationResult>
    CancelDeactivationAsync(
        Guid employeeId,
        EmployeeStatus restoredStatus,
        CancellationToken cancellationToken = default)
    {
        Employee? existingEmployee =
            await _employeeRepository.GetByIdAsync(
                employeeId,
                cancellationToken);

        if (existingEmployee is null)
        {
            return new CancelEmployeeDeactivationResult(
                false,
                "Không tìm thấy nhân viên.");
        }

        if (existingEmployee.Status
            != EmployeeStatus.Inactive)
        {
            return new CancelEmployeeDeactivationResult(
                false,
                "Chỉ có thể hủy ngừng hoạt động đối với nhân viên đã ngừng hoạt động.");
        }

        if (!existingEmployee.TerminationDate.HasValue)
        {
            return new CancelEmployeeDeactivationResult(
                false,
                "Không thể hủy ngừng hoạt động vì hồ sơ chưa có ngày nghỉ việc.");
        }

        if (restoredStatus is not EmployeeStatus.Active
            and not EmployeeStatus.OnLeave)
        {
            return new CancelEmployeeDeactivationResult(
                false,
                "Trạng thái khôi phục phải là Đang làm việc hoặc Nghỉ phép.");
        }

        EmploymentHistory employmentHistory =
            await _employmentHistoryRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        EmploymentPeriod reopenedPeriod;

        try
        {
            reopenedPeriod =
                employmentHistory.ReopenLatestPeriod(
                    existingEmployee
                        .TerminationDate
                        .Value);
        }
        catch (InvalidOperationException exception)
        {
            return new CancelEmployeeDeactivationResult(
                false,
                exception.Message);
        }

        var restoredEmployee =
            new Employee(
                existingEmployee.Id,
                existingEmployee.EmployeeCode,
                existingEmployee.FullName,
                existingEmployee.Email,
                existingEmployee.PhoneNumber,
                existingEmployee.DateOfBirth,
                existingEmployee.HireDate,
                existingEmployee.Department,
                existingEmployee.Position,
                restoredStatus,
                terminationDate: null);

        await _employmentLifecyclePersistence
            .UpdateEmployeeWithPeriodAsync(
                restoredEmployee,
                reopenedPeriod,
                cancellationToken);

        return new CancelEmployeeDeactivationResult(
            true,
            null);
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(
    EmployeeFilter? filter = null,
    CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Employee> employees =
            await _employeeRepository.GetAllAsync(cancellationToken);

        if (filter is null)
        {
            return employees;
        }

        IEnumerable<Employee> filteredEmployees =
            employees;

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string searchText =
                filter.SearchText.Trim();

            filteredEmployees =
                filteredEmployees.Where(
                    employee =>
                        employee.EmployeeCode.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase)
                        || employee.FullName.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase)
                        || employee.Department.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase)
                        || employee.Position.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Status.HasValue)
        {
            filteredEmployees =
                filteredEmployees.Where(
                    employee =>
                        employee.Status == filter.Status.Value);
        }

        if (filter.RequiresProfileCompletionOnly)
        {
            filteredEmployees =
                filteredEmployees.Where(
                    employee =>
                        employee.RequiresProfileCompletion);
        }

        return filteredEmployees.ToList();
    }

    public async Task<CreateEmployeeResult> CreateEmployeeAsync(
    CreateEmployeeRequest request,
    CancellationToken cancellationToken = default)
    {
        Employee? existingEmployee =
            await _employeeRepository.GetByEmployeeCodeAsync(
                request.EmployeeCode,
                cancellationToken);

        if (existingEmployee is not null)
        {
            return new CreateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Mã nhân viên đã tồn tại.");
        }

        if (request.Status == EmployeeStatus.Inactive)
        {
            return new CreateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Không thể tạo mới nhân viên ở trạng thái ngừng hoạt động.");
        }

        Department? department =
        await _departmentRepository.GetByIdAsync(
        request.DepartmentId,
        cancellationToken);

        if (department is null)
        {
            return new CreateEmployeeResult(
                false,
                "Không tìm thấy phòng ban.");
        }

        if (!department.IsActive)
        {
            return new CreateEmployeeResult(
                false,
                "Phòng ban đã ngừng sử dụng.");
        }

        Position? position =
            await _positionRepository.GetByIdAsync(
                request.PositionId,
                cancellationToken);

        if (position is null)
        {
            return new CreateEmployeeResult(
                false,
                "Không tìm thấy chức danh.");
        }

        if (!position.IsActive)
        {
            return new CreateEmployeeResult(
                false,
                "Chức danh đã ngừng sử dụng.");
        }

        Employee employee;

        try
        {
            employee = new Employee(
                Guid.NewGuid(),
                request.EmployeeCode,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.DateOfBirth,
                request.HireDate,
                department.Name,
                position.Name,
                request.Status,
                departmentId: department.Id,
                positionId: position.Id);
        }
        catch (ArgumentException ex)
        {
            return new CreateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: ex.Message);
        }

        var initialEmploymentPeriod =
        new EmploymentPeriod(
        Guid.NewGuid(),
        employee.Id,
        employee.HireDate);

        await _employmentLifecyclePersistence
            .CreateEmployeeWithPeriodAsync(
                employee,
                initialEmploymentPeriod,
                cancellationToken);

        return new CreateEmployeeResult(
            IsSuccessful: true,
            EmployeeId: employee.Id);
    }

    public async Task<UpdateEmployeeResult> UpdateEmployeeAsync(
    UpdateEmployeeRequest request,
    CancellationToken cancellationToken = default)
    {
        Employee? existingEmployee =
            await _employeeRepository.GetByIdAsync(
                request.EmployeeId,
                cancellationToken);

        if (existingEmployee is null)
        {
            return new UpdateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Không tìm thấy nhân viên.");
        }

        Employee? employeeWithSameCode =
            await _employeeRepository.GetByEmployeeCodeAsync(
                request.EmployeeCode,
                cancellationToken);

        if (employeeWithSameCode is not null
            && employeeWithSameCode.Id != request.EmployeeId)
        {
            return new UpdateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Mã nhân viên đã tồn tại.");
        }

        if (existingEmployee.Status != EmployeeStatus.Inactive
            && request.Status == EmployeeStatus.Inactive)
        {
            return new UpdateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Vui lòng sử dụng chức năng Ngừng hoạt động để ghi nhận ngày nghỉ việc.");
        }

        if (existingEmployee.Status == EmployeeStatus.Inactive
            && request.Status != EmployeeStatus.Inactive)
        {
            return new UpdateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Không thể thay đổi trạng thái của nhân viên đã ngừng hoạt động từ màn hình chỉnh sửa.");
        }

        Department? department =
    await _departmentRepository.GetByIdAsync(
        request.DepartmentId,
        cancellationToken);

        if (department is null)
        {
            return new UpdateEmployeeResult(
                false,
                "Không tìm thấy phòng ban.");
        }

        bool keepsCurrentDepartment =
            existingEmployee.DepartmentId
                == department.Id;

        if (!department.IsActive
            && !keepsCurrentDepartment)
        {
            return new UpdateEmployeeResult(
                false,
                "Phòng ban đã ngừng sử dụng.");
        }

        Position? position =
    await _positionRepository.GetByIdAsync(
        request.PositionId,
        cancellationToken);

        if (position is null)
        {
            return new UpdateEmployeeResult(
                false,
                "Không tìm thấy chức danh.");
        }

        bool keepsCurrentPosition =
            existingEmployee.PositionId
                == position.Id;

        if (!position.IsActive
            && !keepsCurrentPosition)
        {
            return new UpdateEmployeeResult(
                false,
                "Chức danh đã ngừng sử dụng.");
        }

        Employee updatedEmployee;

        try
        {
            updatedEmployee = new Employee(
            request.EmployeeId,
            request.EmployeeCode,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            request.HireDate,
            department.Name,
            position.Name,
            request.Status,
            terminationDate: existingEmployee.TerminationDate,
            departmentId: department.Id,
            positionId: position.Id);
        }
        catch (ArgumentException ex)
        {
            return new UpdateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: ex.Message);
        }

        await _employeeRepository.UpdateAsync(
            updatedEmployee,
            cancellationToken);

        return new UpdateEmployeeResult(
            IsSuccessful: true);
    }

    public async Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
    Guid employeeId,
    DateOnly? terminationDate = null,
    CancellationToken cancellationToken = default)
    {
        Employee? employee =
            await _employeeRepository.GetByIdAsync(
                employeeId,
                cancellationToken);

        if (employee is null)
        {
            return new DeactivateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Không tìm thấy nhân viên.");
        }

        // Không ghi đè lịch sử của employee đã nghỉ việc.
        // Legacy employee có TerminationDate = null cũng được giữ nguyên.
        if (employee.Status == EmployeeStatus.Inactive)
        {
            return new DeactivateEmployeeResult(
                IsSuccessful: true);
        }

        if (!terminationDate.HasValue)
        {
            return new DeactivateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Vui lòng chọn ngày nghỉ việc.");
        }

        DateOnly today =
            DateOnly.FromDateTime(DateTime.Today);

        if (terminationDate.Value > today)
        {
            return new DeactivateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Ngày nghỉ việc không thể ở tương lai.");
        }

        EmploymentHistory employmentHistory =
        await _employmentHistoryRepository
        .GetByEmployeeIdAsync(
            employeeId,
            cancellationToken);

        if (employmentHistory.CurrentPeriod is null)
        {
            return new DeactivateEmployeeResult(
                false,
                "Không tìm thấy giai đoạn làm việc đang mở của nhân viên.");
        }

        EmploymentPeriod closedPeriod;

        try
        {
            closedPeriod =
                employmentHistory.CloseCurrentPeriod(
                    terminationDate.Value);
        }
        catch (ArgumentException exception)
        {
            return new DeactivateEmployeeResult(
                false,
                exception.Message);
        }

        Employee inactiveEmployee;

        try
        {
            inactiveEmployee = new Employee(
                employee.Id,
                employee.EmployeeCode,
                employee.FullName,
                employee.Email,
                employee.PhoneNumber,
                employee.DateOfBirth,
                employee.HireDate,
                employee.Department,
                employee.Position,
                EmployeeStatus.Inactive,
                terminationDate.Value);
        }
        catch (ArgumentException ex)
        {
            return new DeactivateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: ex.Message);
        }

        await _employmentLifecyclePersistence
        .UpdateEmployeeWithPeriodAsync(
        inactiveEmployee,
        closedPeriod,
        cancellationToken);

        return new DeactivateEmployeeResult(
            IsSuccessful: true);
    }
}
