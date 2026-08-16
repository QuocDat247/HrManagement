namespace HrManagement.Domain.Employees.Profiles;

public sealed class EmployeeEmergencyContact
{
    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public string FullName
    {
        get;
    }

    public string Relationship
    {
        get;
    }

    public string PhoneNumber
    {
        get;
    }

    public string? Email
    {
        get;
    }

    public bool IsPrimary
    {
        get;
    }

    public EmployeeEmergencyContact(
        Guid id,
        Guid employeeId,
        string fullName,
        string relationship,
        string phoneNumber,
        string? email = null,
        bool isPrimary = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã liên hệ khẩn cấp không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (string.IsNullOrWhiteSpace(
                fullName))
        {
            throw new ArgumentException(
                "Họ tên người liên hệ là bắt buộc.",
                nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(
                relationship))
        {
            throw new ArgumentException(
                "Mối quan hệ là bắt buộc.",
                nameof(relationship));
        }

        if (string.IsNullOrWhiteSpace(
                phoneNumber))
        {
            throw new ArgumentException(
                "Số điện thoại người liên hệ là bắt buộc.",
                nameof(phoneNumber));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        FullName =
            fullName.Trim();

        Relationship =
            relationship.Trim();

        PhoneNumber =
            phoneNumber.Trim();

        Email =
            NormalizeOptional(
                email);

        IsPrimary =
            isPrimary;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
