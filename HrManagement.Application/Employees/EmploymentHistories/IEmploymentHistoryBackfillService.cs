namespace HrManagement.Application.Employees.EmploymentHistories;

public interface IEmploymentHistoryBackfillService
{
    Task<EmploymentHistoryBackfillResult> BackfillAsync(
        CancellationToken cancellationToken = default);
}
