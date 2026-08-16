using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed class EmployeePersonalProfileService
    : IEmployeePersonalProfileService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmployeePersonalProfileRepository
        _profileRepository;

    public EmployeePersonalProfileService(
        IEmployeeRepository employeeRepository,
        IEmployeePersonalProfileRepository profileRepository)
    {
        _employeeRepository =
            employeeRepository;

        _profileRepository =
            profileRepository;
    }

    public async Task<EmployeePersonalProfileDetails>
        GetProfileAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên.");
        }

        EmployeePersonalProfile? profile =
            await _profileRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        if (profile is null)
        {
            return new EmployeePersonalProfileDetails(
                EmployeeId:
                    employeeId,

                HasProfile:
                    false,

                PreferredName:
                    null,

                Gender:
                    null,

                Nationality:
                    null,

                PlaceOfBirth:
                    null);
        }

        return new EmployeePersonalProfileDetails(
            EmployeeId:
                profile.EmployeeId,

            HasProfile:
                true,

            PreferredName:
                profile.PreferredName,

            Gender:
                profile.Gender,

            Nationality:
                profile.Nationality,

            PlaceOfBirth:
                profile.PlaceOfBirth);
    }

    public async Task<SaveEmployeePersonalProfileResult>
        SaveProfileAsync(
            SaveEmployeePersonalProfileRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.EmployeeId == Guid.Empty)
        {
            return new SaveEmployeePersonalProfileResult(
                IsSuccessful:
                    false,

                ErrorMessage:
                    "Mã nhân viên không hợp lệ.");
        }

        if (request.Gender.HasValue
            && !Enum.IsDefined(
                request.Gender.Value))
        {
            return new SaveEmployeePersonalProfileResult(
                IsSuccessful:
                    false,

                ErrorMessage:
                    "Giới tính không hợp lệ.");
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (employee is null)
        {
            return new SaveEmployeePersonalProfileResult(
                IsSuccessful:
                    false,

                ErrorMessage:
                    "Không tìm thấy nhân viên.");
        }

        var profile =
            new EmployeePersonalProfile(
                request.EmployeeId,
                request.PreferredName,
                request.Gender,
                request.Nationality,
                request.PlaceOfBirth);

        await _profileRepository
            .UpsertAsync(
                profile,
                cancellationToken);

        return new SaveEmployeePersonalProfileResult(
            IsSuccessful:
                true);
    }
}
