using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed record ApplyAttendanceCorrectionRequest(
    Guid AttendanceRecordId,
    AttendanceCorrectionKind Kind,
    Guid? AffectedEventId,
    AttendanceEventType? AfterEventType,
    DateTime? AfterOccurredAtUtc,
    string Reason);
