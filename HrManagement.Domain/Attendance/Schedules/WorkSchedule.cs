namespace HrManagement.Domain.Attendance.Schedules;

public sealed class WorkSchedule
{
    public Guid Id
    {
        get;
    }

    public string Code
    {
        get;
    }

    public string Name
    {
        get;
    }

    public string TimeZoneId
    {
        get;
    }

    public bool IsActive
    {
        get;
    }

    public WorkSchedule(
        Guid id,
        string code,
        string name,
        string timeZoneId,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch làm việc không hợp lệ.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new ArgumentException(
                "Mã lịch làm việc không được để trống.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tên lịch làm việc không được để trống.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(
                timeZoneId))
        {
            throw new ArgumentException(
                "Múi giờ của lịch làm việc không được để trống.",
                nameof(timeZoneId));
        }

        Id =
            id;

        Code =
            code.Trim()
                .ToUpperInvariant();

        Name =
            name.Trim();

        TimeZoneId =
            timeZoneId.Trim();

        IsActive =
            isActive;
    }
}
