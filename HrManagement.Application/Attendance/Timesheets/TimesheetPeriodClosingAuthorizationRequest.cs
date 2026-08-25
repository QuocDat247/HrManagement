using HrManagement.Application.Authentication;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed record TimesheetPeriodClosingAuthorizationRequest(
    AuthenticatedUser Actor,
    int Year,
    int Month);
