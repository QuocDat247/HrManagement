using HrManagement.Application.Employees.Profiles;
using HrManagement.Application.Employees.Profiles.Completion;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeProfileCompletionPolicyTests
{
    private readonly EmployeeProfileCompletionPolicy
        _policy =
            new();

    [Fact]
    public void Evaluate_WhenActiveProfileComplete_ReturnsComplete()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Active);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.True(
            result.IsComplete);

        Assert.False(
            result.RequiresCompletion);

        Assert.Empty(
            result.MissingRequirements);
    }

    [Fact]
    public void Evaluate_WhenCoreEmployeeInformationMissing_ReturnsCoreRequirements()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Active,
                email: null,
                phoneNumber: null,
                dateOfBirth: null,
                useDefaultDateOfBirth: false);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.False(
            result.IsComplete);

        Assert.True(
            result.RequiresCompletion);

        Assert.Equal(
            new[]
            {
                EmployeeProfileRequirement.Email,
                EmployeeProfileRequirement.PhoneNumber,
                EmployeeProfileRequirement.DateOfBirth
            },
            result.MissingRequirements);
    }

    [Fact]
    public void Evaluate_WhenPersonalProfileInformationMissing_ReturnsPersonalRequirements()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Active);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        data =
            data with
            {
                PersonalProfile =
                    new EmployeePersonalProfileDetails(
                        employee.Id,
                        HasProfile: false,
                        PreferredName: null,
                        Gender: null,
                        Nationality: null,
                        PlaceOfBirth: null)
            };

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.False(
            result.IsComplete);

        Assert.True(
            result.RequiresCompletion);

        Assert.Equal(
            new[]
            {
                EmployeeProfileRequirement.Gender,
                EmployeeProfileRequirement.Nationality,
                EmployeeProfileRequirement.PlaceOfBirth
            },
            result.MissingRequirements);
    }

    [Fact]
    public void Evaluate_WhenPermanentAddressMissing_ReturnsPermanentAddressRequirement()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Active);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        EmployeeAddressDetails currentAddress =
            CreateAddress(
                EmployeeAddressType.Current,
                "456 Nguyễn Huệ");

        data =
            data with
            {
                Addresses =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress: null,
                        CurrentAddress:
                            currentAddress)
            };

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.False(
            result.IsComplete);

        Assert.True(
            result.RequiresCompletion);

        EmployeeProfileRequirement missing =
            Assert.Single(
                result.MissingRequirements);

        Assert.Equal(
            EmployeeProfileRequirement.PermanentAddress,
            missing);
    }

    [Fact]
    public void Evaluate_WhenEmergencyAndIdentificationRecordsMissing_ReturnsCollectionRequirements()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Active);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        data =
            data with
            {
                EmergencyContacts =
                    Array.Empty<
                        EmployeeEmergencyContactDetails>(),

                IdentificationRecords =
                    Array.Empty<
                        EmployeeIdentificationRecordDetails>()
            };

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.False(
            result.IsComplete);

        Assert.True(
            result.RequiresCompletion);

        Assert.Equal(
            new[]
            {
                EmployeeProfileRequirement.EmergencyContact,
                EmployeeProfileRequirement.IdentificationRecord
            },
            result.MissingRequirements);
    }

    [Fact]
    public void Evaluate_WhenPreferredNameAndCurrentAddressMissing_RemainsComplete()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Active);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        data =
            data with
            {
                PersonalProfile =
                    new EmployeePersonalProfileDetails(
                        employee.Id,
                        HasProfile: true,
                        PreferredName: null,
                        Gender:
                            EmployeeGender.Male,
                        Nationality:
                            "Việt Nam",
                        PlaceOfBirth:
                            "Hà Nội"),

                Addresses =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress:
                            CreateAddress(
                                EmployeeAddressType.Permanent,
                                "123 Lê Lợi"),
                        CurrentAddress: null)
            };

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.True(
            result.IsComplete);

        Assert.False(
            result.RequiresCompletion);

        Assert.Empty(
            result.MissingRequirements);
    }

    [Fact]
    public void Evaluate_WhenOnLeaveProfileIncomplete_RequiresCompletion()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.OnLeave);

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        data =
            data with
            {
                Addresses =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress: null,
                        CurrentAddress: null)
            };

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.False(
            result.IsComplete);

        Assert.True(
            result.RequiresCompletion);

        EmployeeProfileRequirement missing =
            Assert.Single(
                result.MissingRequirements);

        Assert.Equal(
            EmployeeProfileRequirement.PermanentAddress,
            missing);
    }

    [Fact]
    public void Evaluate_WhenInactiveProfileIncomplete_PreservesMissingButDoesNotRequireCompletion()
    {
        Employee employee =
            CreateEmployee(
                EmployeeStatus.Inactive,
                email: null,
                phoneNumber:
                    "0901234567",
                dateOfBirth:
                    new DateOnly(
                        1995,
                        5,
                        10));

        EmployeeProfileCompletionData data =
            CreateCompleteData(
                employee);

        data =
            data with
            {
                Addresses =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress: null,
                        CurrentAddress: null),

                EmergencyContacts =
                    Array.Empty<
                        EmployeeEmergencyContactDetails>(),

                IdentificationRecords =
                    Array.Empty<
                        EmployeeIdentificationRecordDetails>()
            };

        EmployeeProfileCompletionResult result =
            _policy.Evaluate(
                data);

        Assert.False(
            result.IsComplete);

        Assert.False(
            result.RequiresCompletion);

        Assert.Equal(
            new[]
            {
                EmployeeProfileRequirement.Email,
                EmployeeProfileRequirement.PermanentAddress,
                EmployeeProfileRequirement.EmergencyContact,
                EmployeeProfileRequirement.IdentificationRecord
            },
            result.MissingRequirements);
    }

    private static EmployeeProfileCompletionData
        CreateCompleteData(
            Employee employee)
    {
        var personalProfile =
            new EmployeePersonalProfileDetails(
                employee.Id,
                HasProfile: true,
                PreferredName:
                    "An",
                Gender:
                    EmployeeGender.Male,
                Nationality:
                    "Việt Nam",
                PlaceOfBirth:
                    "Hà Nội");

        var addresses =
            new EmployeeAddressBookDetails(
                employee.Id,
                PermanentAddress:
                    CreateAddress(
                        EmployeeAddressType.Permanent,
                        "123 Lê Lợi"),
                CurrentAddress:
                    CreateAddress(
                        EmployeeAddressType.Current,
                        "456 Nguyễn Huệ"));

        IReadOnlyList<EmployeeEmergencyContactDetails>
            emergencyContacts =
            [
                new EmployeeEmergencyContactDetails(
                    Guid.NewGuid(),
                    "Nguyễn Văn Bình",
                    "Cha",
                    "0901000001",
                    null,
                    IsPrimary: true)
            ];

        IReadOnlyList<EmployeeIdentificationRecordDetails>
            identificationRecords =
            [
                new EmployeeIdentificationRecordDetails(
                    Guid.NewGuid(),
                    EmployeeIdentificationType.NationalId,
                    "012345678901",
                    IssueDate:
                        new DateOnly(
                            2024,
                            1,
                            10),
                    ExpiryDate:
                        new DateOnly(
                            2034,
                            1,
                            10),
                    IssuingAuthority:
                        "Cơ quan A",
                    PlaceOfIssue:
                        "Hà Nội",
                    IssuingCountry:
                        "Việt Nam")
            ];

        return new EmployeeProfileCompletionData(
            employee,
            personalProfile,
            addresses,
            emergencyContacts,
            identificationRecords);
    }

    private static EmployeeAddressDetails CreateAddress(
        EmployeeAddressType type,
        string addressLine)
    {
        return new EmployeeAddressDetails(
            Guid.NewGuid(),
            type,
            addressLine,
            Ward:
                "Phường Test",
            District:
                "Quận Test",
            Province:
                "Hà Nội",
            Country:
                "Việt Nam",
            PostalCode:
                "100000");
    }

    private static Employee CreateEmployee(
    EmployeeStatus status,
    string? email = "an@example.com",
    string? phoneNumber = "0901234567",
    DateOnly? dateOfBirth = default,
    bool useDefaultDateOfBirth = true)
    {
        if (useDefaultDateOfBirth
            && dateOfBirth is null)
        {
            dateOfBirth =
                new DateOnly(
                    1995,
                    5,
                    10);
        }

        DateOnly? terminationDate =
            status == EmployeeStatus.Inactive
                ? new DateOnly(
                    2026,
                    1,
                    15)
                : null;

        return new Employee(
            Guid.NewGuid(),
            "EMP-COMPLETION-001",
            "Nguyễn Văn An",
            email,
            phoneNumber,
            dateOfBirth,
            new DateOnly(
                2025,
                1,
                1),
            "Phòng Nhân sự",
            "Chuyên viên",
            status,
            terminationDate);
    }
}
