namespace HrManagement.Application.Employees.EmploymentHistories;

public sealed record EmploymentHistoryBackfillResult(
    int ScannedEmployees,
    int CreatedPeriods,
    int SkippedExistingHistory,
    int SkippedIncompleteLegacyRecords);
