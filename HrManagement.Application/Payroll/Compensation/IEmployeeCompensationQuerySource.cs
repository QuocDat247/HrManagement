namespace HrManagement.Application.Payroll.Compensation;

public interface IEmployeeCompensationQuerySource
{
    Task<IReadOnlyList<EmployeeCompensationSegment>>
        GetForPeriodAsync(
            IReadOnlyCollection<Guid> employeeIds,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeCompensationSegment>>
        GetHistoryAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);
}
