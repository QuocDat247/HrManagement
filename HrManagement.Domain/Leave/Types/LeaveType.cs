namespace HrManagement.Domain.Leave.Types;

public sealed class LeaveType
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

    public bool IsPaid
    {
        get;
    }

    public bool IsActive
    {
        get;
    }

    public LeaveType(
        Guid id,
        string code,
        string name,
        bool isPaid,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã loại nghỉ phép không hợp lệ.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new ArgumentException(
                "Mã loại nghỉ phép không được để trống.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tên loại nghỉ phép không được để trống.",
                nameof(name));
        }

        Id =
            id;

        Code =
            code.Trim()
                .ToUpperInvariant();

        Name =
            name.Trim();

        IsPaid =
            isPaid;

        IsActive =
            isActive;
    }
}
