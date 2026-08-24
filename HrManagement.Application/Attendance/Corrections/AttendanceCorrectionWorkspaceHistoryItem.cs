using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed record AttendanceCorrectionWorkspaceHistoryItem(
    Guid CorrectionId,
    int Revision,
    AttendanceCorrectionKind Kind,
    Guid AffectedEventId,
    AttendanceEventType? BeforeEventType,
    DateTime? BeforeOccurredAtUtc,
    DateTime? BeforeOccurredAtLocal,
    AttendanceEventType? AfterEventType,
    DateTime? AfterOccurredAtUtc,
    DateTime? AfterOccurredAtLocal,
    string Reason,
    DateTime CorrectedAtUtc,
    string ActorUsername);
