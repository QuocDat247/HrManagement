namespace HrManagement.Application.Attendance.Records;

public interface IAttendanceTimeZoneConverter
{
    DateTime ConvertFromUtc(
        DateTime occurredAtUtc,
        string timeZoneId);
}
