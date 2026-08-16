namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeePersonalProfileService
{
    Task<EmployeePersonalProfileDetails>
        GetProfileAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<SaveEmployeePersonalProfileResult>
        SaveProfileAsync(
            SaveEmployeePersonalProfileRequest request,
            CancellationToken cancellationToken = default);
}
