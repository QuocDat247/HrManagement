using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.Profiles.Completion;

public sealed class EmployeeProfileCompletionPolicy
    : IEmployeeProfileCompletionPolicy
{
    public EmployeeProfileCompletionResult Evaluate(
        EmployeeProfileCompletionData data)
    {
        ArgumentNullException.ThrowIfNull(
            data);

        ArgumentNullException.ThrowIfNull(
            data.Employee);

        ArgumentNullException.ThrowIfNull(
            data.PersonalProfile);

        ArgumentNullException.ThrowIfNull(
            data.Addresses);

        ArgumentNullException.ThrowIfNull(
            data.EmergencyContacts);

        ArgumentNullException.ThrowIfNull(
            data.IdentificationRecords);

        var missingRequirements =
            new List<EmployeeProfileRequirement>();

        EvaluateCoreEmployeeInformation(
            data,
            missingRequirements);

        EvaluatePersonalProfile(
            data,
            missingRequirements);

        EvaluateAddresses(
            data,
            missingRequirements);

        EvaluateEmergencyContacts(
            data,
            missingRequirements);

        EvaluateIdentificationRecords(
            data,
            missingRequirements);

        bool isComplete =
            missingRequirements.Count == 0;

        bool requiresCompletion =
            data.Employee.Status
                != EmployeeStatus.Inactive
            && !isComplete;

        return new EmployeeProfileCompletionResult(
            IsComplete: isComplete,
            RequiresCompletion: requiresCompletion,
            MissingRequirements: missingRequirements);
    }

    private static void EvaluateCoreEmployeeInformation(
        EmployeeProfileCompletionData data,
        ICollection<EmployeeProfileRequirement>
            missingRequirements)
    {
        if (string.IsNullOrWhiteSpace(
                data.Employee.Email))
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.Email);
        }

        if (string.IsNullOrWhiteSpace(
                data.Employee.PhoneNumber))
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.PhoneNumber);
        }

        if (!data.Employee.DateOfBirth.HasValue)
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.DateOfBirth);
        }
    }

    private static void EvaluatePersonalProfile(
        EmployeeProfileCompletionData data,
        ICollection<EmployeeProfileRequirement>
            missingRequirements)
    {
        if (!data.PersonalProfile.Gender.HasValue)
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.Gender);
        }

        if (string.IsNullOrWhiteSpace(
                data.PersonalProfile.Nationality))
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.Nationality);
        }

        if (string.IsNullOrWhiteSpace(
                data.PersonalProfile.PlaceOfBirth))
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.PlaceOfBirth);
        }
    }

    private static void EvaluateAddresses(
        EmployeeProfileCompletionData data,
        ICollection<EmployeeProfileRequirement>
            missingRequirements)
    {
        if (data.Addresses.PermanentAddress is null)
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.PermanentAddress);
        }
    }

    private static void EvaluateEmergencyContacts(
        EmployeeProfileCompletionData data,
        ICollection<EmployeeProfileRequirement>
            missingRequirements)
    {
        if (data.EmergencyContacts.Count == 0)
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.EmergencyContact);
        }
    }

    private static void EvaluateIdentificationRecords(
        EmployeeProfileCompletionData data,
        ICollection<EmployeeProfileRequirement>
            missingRequirements)
    {
        if (data.IdentificationRecords.Count == 0)
        {
            missingRequirements.Add(
                EmployeeProfileRequirement.IdentificationRecord);
        }
    }
}
