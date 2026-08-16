using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed class EmployeeOrganizationTransferService
    : IEmployeeOrganizationTransferService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmploymentHistoryRepository
        _employmentHistoryRepository;

    private readonly IEmployeeOrganizationHistoryRepository
        _organizationHistoryRepository;

    private readonly IDepartmentRepository
        _departmentRepository;

    private readonly IPositionRepository
        _positionRepository;

    private readonly IEmployeeOrganizationTransferPersistence
        _transferPersistence;

    public EmployeeOrganizationTransferService(
        IEmployeeRepository employeeRepository,
        IEmploymentHistoryRepository employmentHistoryRepository,
        IEmployeeOrganizationHistoryRepository organizationHistoryRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        IEmployeeOrganizationTransferPersistence transferPersistence)
    {
        _employeeRepository =
            employeeRepository;

        _employmentHistoryRepository =
            employmentHistoryRepository;

        _organizationHistoryRepository =
            organizationHistoryRepository;

        _departmentRepository =
            departmentRepository;

        _positionRepository =
            positionRepository;

        _transferPersistence =
            transferPersistence;
    }

    public async Task<TransferEmployeeOrganizationResult>
        TransferAsync(
            TransferEmployeeOrganizationRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (request.DepartmentId == Guid.Empty)
        {
            return Failure(
                "Vui lòng chọn phòng ban.");
        }

        if (request.PositionId == Guid.Empty)
        {
            return Failure(
                "Vui lòng chọn chức danh.");
        }

        if (request.EffectiveDate == default)
        {
            return Failure(
                "Ngày điều chuyển không hợp lệ.");
        }

        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Today);

        if (request.EffectiveDate > today)
        {
            return Failure(
                "Ngày điều chuyển không thể ở tương lai.");
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Không tìm thấy nhân viên.");
        }

        if (employee.Status is not EmployeeStatus.Active
            and not EmployeeStatus.OnLeave)
        {
            return Failure(
                "Chỉ có thể điều chuyển nhân viên "
                + "đang làm việc hoặc nghỉ phép.");
        }

        if (!employee.DepartmentId.HasValue
            || !employee.PositionId.HasValue)
        {
            return Failure(
                "Nhân viên chưa có đầy đủ thông tin tổ chức.");
        }

        Department? targetDepartment =
            await _departmentRepository
                .GetByIdAsync(
                    request.DepartmentId,
                    cancellationToken);

        if (targetDepartment is null)
        {
            return Failure(
                "Không tìm thấy phòng ban.");
        }

        if (!targetDepartment.IsActive)
        {
            return Failure(
                "Phòng ban đã ngừng sử dụng.");
        }

        Position? targetPosition =
            await _positionRepository
                .GetByIdAsync(
                    request.PositionId,
                    cancellationToken);

        if (targetPosition is null)
        {
            return Failure(
                "Không tìm thấy chức danh.");
        }

        if (!targetPosition.IsActive)
        {
            return Failure(
                "Chức danh đã ngừng sử dụng.");
        }

        if (employee.DepartmentId.Value
                == targetDepartment.Id
            && employee.PositionId.Value
                == targetPosition.Id)
        {
            return Failure(
                "Điều chuyển phải thay đổi phòng ban, "
                + "chức danh hoặc cả hai.");
        }

        EmploymentHistory employmentHistory =
            await _employmentHistoryRepository
                .GetByEmployeeIdAsync(
                    employee.Id,
                    cancellationToken);

        EmploymentPeriod? currentPeriod =
            employmentHistory.CurrentPeriod;

        if (currentPeriod is null)
        {
            return Failure(
                "Không tìm thấy giai đoạn làm việc "
                + "đang mở của nhân viên.");
        }

        EmployeeOrganizationHistory organizationHistory =
            await _organizationHistoryRepository
                .GetByEmployeeIdAsync(
                    employee.Id,
                    cancellationToken);

        EmployeeOrganizationAssignment? currentAssignment =
            organizationHistory.CurrentAssignment;

        if (currentAssignment is null)
        {
            return Failure(
                "Không tìm thấy phân công tổ chức "
                + "đang mở của nhân viên.");
        }

        if (currentAssignment.EmploymentPeriodId
            != currentPeriod.Id)
        {
            return Failure(
                "Phân công hiện tại không khớp với "
                + "giai đoạn làm việc đang mở.");
        }

        if (currentAssignment.DepartmentId
                != employee.DepartmentId.Value
            || currentAssignment.PositionId
                != employee.PositionId.Value)
        {
            return Failure(
                "Phân công hiện tại không khớp với "
                + "tổ chức của nhân viên.");
        }

        EmployeeOrganizationAssignment newAssignment;

        try
        {
            newAssignment =
                organizationHistory.Transfer(
                    Guid.NewGuid(),
                    targetDepartment.Id,
                    targetDepartment.Code,
                    targetDepartment.Name,
                    targetPosition.Id,
                    targetPosition.Code,
                    targetPosition.Name,
                    request.EffectiveDate);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        EmployeeOrganizationAssignment closedAssignment =
            organizationHistory.Assignments[
                organizationHistory.Assignments.Count - 2];

        var transferredEmployee =
            new Employee(
                employee.Id,
                employee.EmployeeCode,
                employee.FullName,
                employee.Email,
                employee.PhoneNumber,
                employee.DateOfBirth,
                employee.HireDate,
                targetDepartment.Name,
                targetPosition.Name,
                employee.Status,
                employee.TerminationDate,
                departmentId:
                    targetDepartment.Id,
                positionId:
                    targetPosition.Id);

        await _transferPersistence
            .TransferEmployeeOrganizationAsync(
                transferredEmployee,
                closedAssignment,
                newAssignment,
                cancellationToken);

        return new TransferEmployeeOrganizationResult(
            IsSuccessful: true);
    }

    private static TransferEmployeeOrganizationResult
        Failure(
            string errorMessage)
    {
        return new TransferEmployeeOrganizationResult(
            IsSuccessful: false,
            ErrorMessage: errorMessage);
    }
}
