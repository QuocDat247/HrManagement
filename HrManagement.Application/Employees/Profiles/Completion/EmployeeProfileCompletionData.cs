using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.Profiles.Completion;

public sealed record EmployeeProfileCompletionData(
    Employee Employee,
    EmployeePersonalProfileDetails PersonalProfile,
    EmployeeAddressBookDetails Addresses,
    IReadOnlyList<EmployeeEmergencyContactDetails> EmergencyContacts,
    IReadOnlyList<EmployeeIdentificationRecordDetails> IdentificationRecords);
