namespace HrManagement.Application.Payroll.Periods;

public sealed record ClosePayrollPeriodResult(
    bool IsSuccessful,
    Guid? PayrollPeriodId = null,
    int SnapshotCount = 0,
    DateTime? ClosedAtUtc = null,
    string? ErrorMessage = null);
