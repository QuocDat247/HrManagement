using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.EmploymentHistories;

public sealed class EmploymentHistoryService
    : IEmploymentHistoryService
{
    private readonly IEmploymentHistoryRepository
        _employmentHistoryRepository;

    public EmploymentHistoryService(
        IEmploymentHistoryRepository employmentHistoryRepository)
    {
        _employmentHistoryRepository =
            employmentHistoryRepository;
    }

    public async Task<EmployeeEmploymentHistoryDetails>
        GetHistoryAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        EmploymentHistory history =
            await _employmentHistoryRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        List<EmploymentHistoryPeriodItem> periods =
            history.Periods
                .Select((period, index) =>
                    new EmploymentHistoryPeriodItem(
                        Id: period.Id,
                        SequenceNumber: index + 1,
                        StartDate: period.StartDate,
                        EndDate: period.EndDate,
                        IsOpen: period.IsOpen))
                .ToList();

        return new EmployeeEmploymentHistoryDetails(
            EmployeeId: history.EmployeeId,
            Periods: periods);
    }
}
