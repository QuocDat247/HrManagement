namespace HrManagement.Application.Payroll.Periods;

public sealed record ClosePayrollPeriodRequest(
    int Year,
    int Month);
