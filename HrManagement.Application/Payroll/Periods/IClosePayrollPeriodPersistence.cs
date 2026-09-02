using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;

namespace HrManagement.Application.Payroll.Periods;

public interface IClosePayrollPeriodPersistence
{
    Task PersistAsync(
        PayrollPeriod payrollPeriod,
        IReadOnlyList<PayrollEmployeeSnapshot> snapshots,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default);
}
