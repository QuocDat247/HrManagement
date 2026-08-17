namespace HrManagement.Domain.Employees.Profiles;

public sealed class EmployeeIdentificationRecord
{
    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public EmployeeIdentificationType Type
    {
        get;
    }

    public string DocumentNumber
    {
        get;
    }

    public DateOnly? IssueDate
    {
        get;
    }

    public DateOnly? ExpiryDate
    {
        get;
    }

    public string? IssuingAuthority
    {
        get;
    }

    public string? PlaceOfIssue
    {
        get;
    }

    public string? IssuingCountry
    {
        get;
    }

    public EmployeeIdentificationRecord(
        Guid id,
        Guid employeeId,
        EmployeeIdentificationType type,
        string documentNumber,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null,
        string? issuingAuthority = null,
        string? placeOfIssue = null,
        string? issuingCountry = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giấy tờ không hợp lệ.",
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
                nameof(type),
                "Loại giấy tờ không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(
                documentNumber))
        {
            throw new ArgumentException(
                "Số giấy tờ là bắt buộc.",
                nameof(documentNumber));
        }

        if (issueDate.HasValue
            && expiryDate.HasValue
            && expiryDate.Value < issueDate.Value)
        {
            throw new ArgumentException(
                "Ngày hết hạn không được trước ngày cấp.",
                nameof(expiryDate));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        Type =
            type;

        DocumentNumber =
            documentNumber.Trim();

        IssueDate =
            issueDate;

        ExpiryDate =
            expiryDate;

        IssuingAuthority =
            NormalizeOptional(
                issuingAuthority);

        PlaceOfIssue =
            NormalizeOptional(
                placeOfIssue);

        IssuingCountry =
            NormalizeOptional(
                issuingCountry);
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
