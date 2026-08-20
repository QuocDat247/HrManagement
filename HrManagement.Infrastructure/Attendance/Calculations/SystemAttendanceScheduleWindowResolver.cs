using HrManagement.Application.Attendance.Calculations;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Infrastructure.Attendance.Calculations;

public sealed class SystemAttendanceScheduleWindowResolver
    : IAttendanceScheduleWindowResolver
{
    public AttendanceScheduleWindow? Resolve(
        AttendanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        if (!record.IsWorkingDay)
        {
            return null;
        }

        if (!record.ExpectedStartTime.HasValue
            || !record.ExpectedEndTime.HasValue)
        {
            throw new InvalidOperationException(
                "Ngày làm việc không có đầy đủ giờ dự kiến.");
        }

        TimeZoneInfo timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                record.TimeZoneId);

        DateOnly endDate =
            record.IsOvernight
                ? record.WorkDate.AddDays(
                    1)
                : record.WorkDate;

        DateTime localStart =
            DateTime.SpecifyKind(
                record.WorkDate.ToDateTime(
                    record.ExpectedStartTime.Value),
                DateTimeKind.Unspecified);

        DateTime localEnd =
            DateTime.SpecifyKind(
                endDate.ToDateTime(
                    record.ExpectedEndTime.Value),
                DateTimeKind.Unspecified);

        EnsureResolvableLocalTime(
            timeZone,
            localStart,
            "Giờ bắt đầu dự kiến");

        EnsureResolvableLocalTime(
            timeZone,
            localEnd,
            "Giờ kết thúc dự kiến");

        DateTime expectedStartAtUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localStart,
                timeZone);

        DateTime expectedEndAtUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localEnd,
                timeZone);

        return new AttendanceScheduleWindow(
            expectedStartAtUtc,
            expectedEndAtUtc);
    }

    private static void EnsureResolvableLocalTime(
        TimeZoneInfo timeZone,
        DateTime localTime,
        string fieldName)
    {
        if (timeZone.IsInvalidTime(
                localTime))
        {
            throw new InvalidOperationException(
                $"{fieldName} rơi vào thời điểm không tồn tại do chuyển đổi múi giờ.");
        }

        if (timeZone.IsAmbiguousTime(
                localTime))
        {
            throw new InvalidOperationException(
                $"{fieldName} bị mơ hồ do chuyển đổi múi giờ.");
        }
    }
}
