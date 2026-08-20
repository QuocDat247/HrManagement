namespace HrManagement.Application.Attendance.Calculations;

public interface IAttendanceRecalculationService
{
    Task<RecalculateAttendanceResult> RecalculateAsync(
        RecalculateAttendanceRequest request,
        CancellationToken cancellationToken = default);
}
