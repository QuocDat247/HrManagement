namespace HrManagement.Domain.Authorization.Roles;

public sealed class Role
{
    public Guid Id { get; }

    public string Name
    {
        get;
        private set;
    }

    public string NormalizedName
    {
        get;
        private set;
    }

    public string? Description
    {
        get;
        private set;
    }

    public bool IsActive { get; }

    public Role(
        Guid id,
        string name,
        string? description = null,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã định danh vai trò không hợp lệ.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tên vai trò không được để trống.",
                nameof(name));
        }

        string trimmedName =
            name.Trim();

        if (trimmedName.Length > 100)
        {
            throw new ArgumentException(
                "Tên vai trò không được vượt quá 100 ký tự.",
                nameof(name));
        }

        string? normalizedDescription =
            string.IsNullOrWhiteSpace(
                description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length > 500)
        {
            throw new ArgumentException(
                "Mô tả vai trò không được vượt quá 500 ký tự.",
                nameof(description));
        }

        Id =
            id;

        Name =
            trimmedName;

        NormalizedName =
            trimmedName.ToUpperInvariant();

        Description =
            normalizedDescription;

        IsActive =
            isActive;
    }

    public void UpdateDetails(
        string name,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tên vai trò không được để trống.",
                nameof(name));
        }

        string trimmedName =
            name.Trim();

        if (trimmedName.Length > 100)
        {
            throw new ArgumentException(
                "Tên vai trò không được vượt quá 100 ký tự.",
                nameof(name));
        }

        string? normalizedDescription =
            string.IsNullOrWhiteSpace(
                description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length > 500)
        {
            throw new ArgumentException(
                "Mô tả vai trò không được vượt quá 500 ký tự.",
                nameof(description));
        }

        Name =
            trimmedName;

        NormalizedName =
            trimmedName.ToUpperInvariant();

        Description =
            normalizedDescription;
    }
}
