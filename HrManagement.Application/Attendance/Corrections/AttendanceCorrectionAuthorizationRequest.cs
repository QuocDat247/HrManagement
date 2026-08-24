using HrManagement.Application.Authentication;
using HrManagement.Domain.Attendance.Corrections;

namespace HrManagement.Application.Attendance.Corrections;

public sealed record AttendanceCorrectionAuthorizationRequest(
    AuthenticatedUser Actor,
    Guid AttendanceRecordId,
    Guid EmployeeId,
    AttendanceCorrectionKind Kind);
