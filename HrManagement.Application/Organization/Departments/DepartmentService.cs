using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Application.Organization.Departments;

public sealed class DepartmentService
    : IDepartmentService
{
    private readonly IDepartmentRepository
        _departmentRepository;

    public DepartmentService(
        IDepartmentRepository departmentRepository)
    {
        _departmentRepository =
            departmentRepository;
    }

    public async Task<IReadOnlyList<Department>>
        GetDepartmentsAsync(
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Department> departments =
            await _departmentRepository
                .GetAllAsync(cancellationToken);

        return departments
            .OrderBy(department => department.Name)
            .ThenBy(department => department.Code)
            .ToList();
    }

    public async Task<DepartmentOperationResult>
        CreateDepartmentAsync(
            CreateDepartmentRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Department department;

        try
        {
            department =
                new Department(
                    Guid.NewGuid(),
                    request.Code,
                    request.Name);
        }
        catch (ArgumentException exception)
        {
            return new DepartmentOperationResult(
                false,
                exception.Message);
        }

        Department? existingDepartment =
            await _departmentRepository
                .GetByCodeAsync(
                    department.Code,
                    cancellationToken);

        if (existingDepartment is not null)
        {
            return new DepartmentOperationResult(
                false,
                "Mã phòng ban đã tồn tại.");
        }

        await _departmentRepository.AddAsync(
            department,
            cancellationToken);

        return new DepartmentOperationResult(
            true,
            null);
    }

    public async Task<DepartmentOperationResult>
        UpdateDepartmentAsync(
            UpdateDepartmentRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Department? existingDepartment =
            await _departmentRepository
                .GetByIdAsync(
                    request.DepartmentId,
                    cancellationToken);

        if (existingDepartment is null)
        {
            return new DepartmentOperationResult(
                false,
                "Không tìm thấy phòng ban.");
        }

        Department updatedDepartment;

        try
        {
            updatedDepartment =
                new Department(
                    existingDepartment.Id,
                    request.Code,
                    request.Name,
                    existingDepartment.IsActive);
        }
        catch (ArgumentException exception)
        {
            return new DepartmentOperationResult(
                false,
                exception.Message);
        }

        Department? departmentWithSameCode =
            await _departmentRepository
                .GetByCodeAsync(
                    updatedDepartment.Code,
                    cancellationToken);

        if (departmentWithSameCode is not null
            && departmentWithSameCode.Id
                != existingDepartment.Id)
        {
            return new DepartmentOperationResult(
                false,
                "Mã phòng ban đã tồn tại.");
        }

        await _departmentRepository.UpdateAsync(
            updatedDepartment,
            cancellationToken);

        return new DepartmentOperationResult(
            true,
            null);
    }

    public async Task<DepartmentOperationResult>
        DeactivateDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
    {
        Department? existingDepartment =
            await _departmentRepository
                .GetByIdAsync(
                    departmentId,
                    cancellationToken);

        if (existingDepartment is null)
        {
            return new DepartmentOperationResult(
                false,
                "Không tìm thấy phòng ban.");
        }

        if (!existingDepartment.IsActive)
        {
            return new DepartmentOperationResult(
                true,
                null);
        }

        var deactivatedDepartment =
            new Department(
                existingDepartment.Id,
                existingDepartment.Code,
                existingDepartment.Name,
                false);

        await _departmentRepository.UpdateAsync(
            deactivatedDepartment,
            cancellationToken);

        return new DepartmentOperationResult(
            true,
            null);
    }

    public async Task<DepartmentOperationResult>
        ReactivateDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
    {
        Department? existingDepartment =
            await _departmentRepository
                .GetByIdAsync(
                    departmentId,
                    cancellationToken);

        if (existingDepartment is null)
        {
            return new DepartmentOperationResult(
                false,
                "Không tìm thấy phòng ban.");
        }

        if (existingDepartment.IsActive)
        {
            return new DepartmentOperationResult(
                true,
                null);
        }

        var reactivatedDepartment =
            new Department(
                existingDepartment.Id,
                existingDepartment.Code,
                existingDepartment.Name,
                true);

        await _departmentRepository.UpdateAsync(
            reactivatedDepartment,
            cancellationToken);

        return new DepartmentOperationResult(
            true,
            null);
    }
}
