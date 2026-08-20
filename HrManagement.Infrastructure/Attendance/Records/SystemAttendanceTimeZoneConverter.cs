using HrManagement.Application.Attendance.Records;

namespace HrManagement.Infrastructure.Attendance.Records;

public sealed class SystemAttendanceTimeZoneConverter
    : IAttendanceTimeZoneConverter
{
    public DateTime ConvertFromUtc(
        DateTime occurredAtUtc,
        string timeZoneId)
    {
        if (occurredAtUtc == default)
        {
            throw new ArgumentException(
                "Thời điểm chấm công không hợp lệ.",
                nameof(occurredAtUtc));
        }

        if (occurredAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm chấm công phải ở UTC.",
                nameof(occurredAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                timeZoneId))
        {
            throw new ArgumentException(
                "Múi giờ không được để trống.",
                nameof(timeZoneId));
        }

        TimeZoneInfo timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                timeZoneId.Trim());

        return TimeZoneInfo.ConvertTimeFromUtc(
            occurredAtUtc,
            timeZone);
    }
}
