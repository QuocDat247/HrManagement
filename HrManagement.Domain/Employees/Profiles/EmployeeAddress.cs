namespace HrManagement.Domain.Employees.Profiles;

public sealed class EmployeeAddress
{
    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public EmployeeAddressType Type
    {
        get;
    }

    public string AddressLine
    {
        get;
    }

    public string? Ward
    {
        get;
    }

    public string? District
    {
        get;
    }

    public string? Province
    {
        get;
    }

    public string Country
    {
        get;
    }

    public string? PostalCode
    {
        get;
    }

    public EmployeeAddress(
        Guid id,
        Guid employeeId,
        EmployeeAddressType type,
        string addressLine,
        string? ward = null,
        string? district = null,
        string? province = null,
        string country = "Việt Nam",
        string? postalCode = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã địa chỉ không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type));
        }

        if (string.IsNullOrWhiteSpace(addressLine))
        {
            throw new ArgumentException(
                "Địa chỉ chi tiết là bắt buộc.",
                nameof(addressLine));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException(
                "Quốc gia là bắt buộc.",
                nameof(country));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        Type =
            type;

        AddressLine =
            addressLine.Trim();

        Ward =
            NormalizeOptional(
                ward);

        District =
            NormalizeOptional(
                district);

        Province =
            NormalizeOptional(
                province);

        Country =
            country.Trim();

        PostalCode =
            NormalizeOptional(
                postalCode);
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
