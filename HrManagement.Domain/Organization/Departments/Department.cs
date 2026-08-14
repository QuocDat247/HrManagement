namespace HrManagement.Domain.Organization.Departments;

public sealed class Department
{
    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; }

    public bool IsActive { get; }

    public Department(
        Guid id,
        string code,
        string name,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã định danh phòng ban không hợp lệ.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Mã phòng ban không được để trống.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Tên phòng ban không được để trống.",
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
