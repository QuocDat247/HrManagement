namespace HrManagement.Application.Payroll.Calculations;

public sealed record ApprovedOvertimePayrollItem(
    Guid OvertimeRequestId,
    Guid EmployeeId,
    DateOnly WorkDate,
    int ApprovedMinutes);
