using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
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

        IEnumerable<Employee> query = employees;

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string searchText = filter.SearchText.Trim();

            query = query.Where(employee =>
                employee.EmployeeCode.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                ||
                employee.FullName.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                ||
                employee.Department.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                ||
                employee.Position.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(
                employee => employee.Status == filter.Status.Value);
        }

        return query.ToList();
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
                request.Department,
                request.Position,
                request.Status);
        }
        catch (ArgumentException ex)
        {
            return new CreateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: ex.Message);
        }

        await _employeeRepository.AddAsync(
            employee,
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
                request.Id,
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
            && employeeWithSameCode.Id != request.Id)
        {
            return new UpdateEmployeeResult(
                IsSuccessful: false,
                ErrorMessage: "Mã nhân viên đã tồn tại.");
        }

        Employee updatedEmployee;

        try
        {
            updatedEmployee = new Employee(
                request.Id,
                request.EmployeeCode,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.DateOfBirth,
                request.HireDate,
                request.Department,
                request.Position,
                request.Status);
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

        if (employee.Status == EmployeeStatus.Inactive)
        {
            return new DeactivateEmployeeResult(
                IsSuccessful: true);
        }

        var inactiveEmployee = new Employee(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.HireDate,
            employee.Department,
            employee.Position,
            EmployeeStatus.Inactive);

        await _employeeRepository.UpdateAsync(
            inactiveEmployee,
            cancellationToken);

        return new DeactivateEmployeeResult(
            IsSuccessful: true);
    }
}
