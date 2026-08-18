using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.Profiles.Completion;

public sealed class EmployeeProfileCompletionService
    : IEmployeeProfileCompletionService
{
    private readonly IEmployeePersonalProfileService
        _personalProfileService;

    private readonly IEmployeeAddressService
        _addressService;

    private readonly IEmployeeEmergencyContactService
        _emergencyContactService;

    private readonly IEmployeeIdentificationRecordService
        _identificationRecordService;

    private readonly IEmployeeProfileCompletionPolicy
        _completionPolicy;

    public EmployeeProfileCompletionService(
        IEmployeePersonalProfileService personalProfileService,
        IEmployeeAddressService addressService,
        IEmployeeEmergencyContactService emergencyContactService,
        IEmployeeIdentificationRecordService identificationRecordService,
        IEmployeeProfileCompletionPolicy completionPolicy)
    {
        _personalProfileService =
            personalProfileService;

        _addressService =
            addressService;

        _emergencyContactService =
            emergencyContactService;

        _identificationRecordService =
            identificationRecordService;

        _completionPolicy =
            completionPolicy;
    }

    public async Task<EmployeeProfileCompletionResult>
        EvaluateAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        Task<EmployeePersonalProfileDetails>
            personalProfileTask =
                _personalProfileService
                    .GetProfileAsync(
                        employee.Id,
                        cancellationToken);

        Task<EmployeeAddressBookDetails>
            addressesTask =
                _addressService
                    .GetAddressesAsync(
                        employee.Id,
                        cancellationToken);

        Task<IReadOnlyList<
            EmployeeEmergencyContactDetails>>
            emergencyContactsTask =
                _emergencyContactService
                    .GetContactsAsync(
                        employee.Id,
                        cancellationToken);

        Task<IReadOnlyList<
            EmployeeIdentificationRecordDetails>>
            identificationRecordsTask =
                _identificationRecordService
                    .GetRecordsAsync(
                        employee.Id,
                        cancellationToken);

        await Task.WhenAll(
            personalProfileTask,
            addressesTask,
            emergencyContactsTask,
            identificationRecordsTask);

        var data =
            new EmployeeProfileCompletionData(
                employee,
                await personalProfileTask,
                await addressesTask,
                await emergencyContactsTask,
                await identificationRecordsTask);

        return _completionPolicy
            .Evaluate(
                data);
    }
}
