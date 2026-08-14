namespace HrManagement.Domain.Organization.Positions;

public sealed class Position
{
    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; }

    public bool IsActive { get; }

    public Position(
        Guid id,
        string code,
        string name,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã định danh chức danh không hợp lệ.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Mã chức danh không được để trống.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Tên chức danh không được để trống.",
                nameof(name));
        }

        Id = id;

        Code =
            code.Trim()
                .ToUpperInvariant();

        Name =
            name.Trim();

        IsActive =
            isActive;
    }
}
