using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeeIdentificationRecordRepository
{
    Task<IReadOnlyList<EmployeeIdentificationRecord>>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task UpsertAsync(
        EmployeeIdentificationRecord record,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid employeeId,
        Guid recordId,
        CancellationToken cancellationToken = default);
}
