using HrManagement.Domain.Attendance.Expectations;

namespace HrManagement.Application.Attendance.Expectations;

public sealed record ResolvedWorkExpectation(
    Guid WorkScheduleId,
    DateOnly WorkDate,
    bool IsWorkingDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int BreakMinutes,
    int PlannedMinutes,
    bool IsOvernight,
    WorkExpectationSource Source,
    Guid SourceId,
    string? SourceName);
