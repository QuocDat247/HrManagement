namespace HrManagement.Application.Employees.EmploymentHistories;

public interface IEmploymentHistoryService
{
    Task<EmployeeEmploymentHistoryDetails> GetHistoryAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
