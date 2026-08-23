namespace HrManagement.Domain.Attendance.Calendars;

public sealed class HolidayCalendarDay
{
    public Guid Id
    {
        get;
    }

    public DateOnly Date
    {
        get;
    }

    public string Name
    {
        get;
        private set;
    }

    public bool IsActive
    {
        get;
        private set;
    }

    public HolidayCalendarDay(
        Guid id,
        DateOnly date,
        string name,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã ngày lễ không hợp lệ.",
                nameof(id));
        }

        if (date == default)
        {
            throw new ArgumentException(
                "Ngày lễ không hợp lệ.",
                nameof(date));
        }

        Name =
            NormalizeName(
                name);

        Id =
            id;

        Date =
            date;

        IsActive =
            isActive;
    }

    public void Rename(
        string name)
    {
        Name =
            NormalizeName(
                name);
    }

    public void Deactivate()
    {
        IsActive =
            false;
    }

    public void Reactivate()
    {
        IsActive =
            true;
    }

    private static string NormalizeName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tên ngày lễ không được để trống.",
                nameof(name));
        }

        return name.Trim();
    }
}
