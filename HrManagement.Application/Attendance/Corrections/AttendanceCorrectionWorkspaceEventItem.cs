using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed record AttendanceCorrectionWorkspaceEventItem(
    Guid EventId,
    AttendanceEventType EventType,
    DateTime OccurredAtUtc,
    DateTime OccurredAtLocal,
    bool IsManual,
    bool IsCorrected,
    int? LastCorrectionRevision);
