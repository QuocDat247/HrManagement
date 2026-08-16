namespace HrManagement.Domain.Employees.Profiles;

public sealed class EmployeePersonalProfile
{
    public Guid EmployeeId
    {
        get;
    }

    public string? PreferredName
    {
        get;
    }

    public EmployeeGender? Gender
    {
        get;
    }

    public string? Nationality
    {
        get;
    }

    public string? PlaceOfBirth
    {
        get;
    }

    public EmployeePersonalProfile(
        Guid employeeId,
        string? preferredName = null,
        EmployeeGender? gender = null,
        string? nationality = null,
        string? placeOfBirth = null)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (gender.HasValue
            && !Enum.IsDefined(gender.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(gender));
        }

        EmployeeId =
            employeeId;

        PreferredName =
            NormalizeOptional(
                preferredName);

        Gender =
            gender;

        Nationality =
            NormalizeOptional(
                nationality);

        PlaceOfBirth =
            NormalizeOptional(
                placeOfBirth);
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
