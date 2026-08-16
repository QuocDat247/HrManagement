namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeeEmergencyContactService
{
    Task<IReadOnlyList<EmployeeEmergencyContactDetails>>
        GetContactsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<EmployeeEmergencyContactOperationResult>
        SaveContactAsync(
            SaveEmployeeEmergencyContactRequest request,
            CancellationToken cancellationToken = default);

    Task<EmployeeEmergencyContactOperationResult>
        DeleteContactAsync(
            Guid employeeId,
            Guid contactId,
            CancellationToken cancellationToken = default);
}
