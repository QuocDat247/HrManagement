namespace HrManagement.Application.Payroll.Calculations;

public interface IApprovedOvertimePayrollSource
{
    Task<IReadOnlyList<ApprovedOvertimePayrollItem>>
        GetApprovedAsync(
            IReadOnlyCollection<Guid> employeeIds,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken = default);
}
