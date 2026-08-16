using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed class EmployeeEmergencyContactService
    : IEmployeeEmergencyContactService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmployeeEmergencyContactRepository
        _contactRepository;

    public EmployeeEmergencyContactService(
        IEmployeeRepository employeeRepository,
        IEmployeeEmergencyContactRepository contactRepository)
    {
        _employeeRepository =
            employeeRepository;

        _contactRepository =
            contactRepository;
    }

    public async Task<IReadOnlyList<EmployeeEmergencyContactDetails>>
        GetContactsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        var employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            throw new KeyNotFoundException(
                "Nhân viên không tồn tại.");
        }

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await _contactRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        return contacts
            .Select(Map)
            .ToList();
    }

    public async Task<EmployeeEmergencyContactOperationResult>
        SaveContactAsync(
            SaveEmployeeEmergencyContactRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (request.ContactId == Guid.Empty)
        {
            return Failure(
                "Mã liên hệ khẩn cấp không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(
                request.FullName))
        {
            return Failure(
                "Họ tên người liên hệ là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Relationship))
        {
            return Failure(
                "Mối quan hệ là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(
                request.PhoneNumber))
        {
            return Failure(
                "Số điện thoại người liên hệ là bắt buộc.");
        }

        var employee =
            await _employeeRepository
                .GetByIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Nhân viên không tồn tại.");
        }

        Guid contactId;

        if (request.ContactId.HasValue)
        {
            IReadOnlyList<EmployeeEmergencyContact> contacts =
                await _contactRepository
                    .GetByEmployeeIdAsync(
                        request.EmployeeId,
                        cancellationToken);

            bool contactExists =
                contacts.Any(
                    contact =>
                        contact.Id ==
                        request.ContactId.Value);

            if (!contactExists)
            {
                return Failure(
                    "Liên hệ khẩn cấp không tồn tại.");
            }

            contactId =
                request.ContactId.Value;
        }
        else
        {
            contactId =
                Guid.NewGuid();
        }

        var contact =
            new EmployeeEmergencyContact(
                contactId,
                request.EmployeeId,
                request.FullName,
                request.Relationship,
                request.PhoneNumber,
                request.Email,
                request.IsPrimary);

        await _contactRepository
            .UpsertAsync(
                contact,
                cancellationToken);

        return new EmployeeEmergencyContactOperationResult(
            IsSuccessful: true,
            ContactId: contactId);
    }

    public async Task<EmployeeEmergencyContactOperationResult>
        DeleteContactAsync(
            Guid employeeId,
            Guid contactId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (contactId == Guid.Empty)
        {
            return Failure(
                "Mã liên hệ khẩn cấp không hợp lệ.");
        }

        var employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Nhân viên không tồn tại.");
        }

        await _contactRepository
            .DeleteAsync(
                employeeId,
                contactId,
                cancellationToken);

        return new EmployeeEmergencyContactOperationResult(
            IsSuccessful: true);
    }

    private static EmployeeEmergencyContactDetails Map(
        EmployeeEmergencyContact contact)
    {
        return new EmployeeEmergencyContactDetails(
            contact.Id,
            contact.FullName,
            contact.Relationship,
            contact.PhoneNumber,
            contact.Email,
            contact.IsPrimary);
    }

    private static EmployeeEmergencyContactOperationResult Failure(
        string errorMessage)
    {
        return new EmployeeEmergencyContactOperationResult(
            IsSuccessful: false,
            ErrorMessage: errorMessage);
    }
}
