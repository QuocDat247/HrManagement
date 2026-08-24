using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed record EffectiveAttendanceEvent(
    Guid EventId,
    Guid AttendanceRecordId,
    Guid EmployeeId,
    AttendanceEventType EventType,
    DateTime OccurredAtUtc,
    bool IsManual,
    bool IsCorrected,
    int? LastCorrectionRevision);
