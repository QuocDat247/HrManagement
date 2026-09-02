namespace HrManagement.Application.Payroll.Calculations;

public enum PayrollCalculationIssueCode
{
    TimesheetNotClosed = 1,
    MissingCompensation = 2,
    OverlappingCompensation = 3,
    MixedCompensationCurrency = 4,
    OvertimeWithoutTimesheetDay = 5,
    OvertimeRequiresReview = 6,
    OvertimePayRateNotConfigured = 7
}
