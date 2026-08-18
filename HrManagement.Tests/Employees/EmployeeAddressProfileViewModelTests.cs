using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Tests.TestDoubles;
using HrManagement.Application.Employees.Profiles.Completion;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeAddressProfileViewModelTests
{
    [Fact]
    public void AddressSlot_LoadExistingAddress_MapsValuesAndEnablesDelete()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubAddressService();

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeAddressSlotViewModel(
                service,
                EmployeeAddressType.Current,
                "Địa chỉ thường trú",
                confirmationDialogService);

        var address =
            new EmployeeAddressDetails(
                Guid.NewGuid(),
                EmployeeAddressType.Current,
                "123 Nguyễn Trãi",
                "Phường A",
                "Quận B",
                "Hà Nội",
                "Việt Nam",
                "100000");

        viewModel.Load(
            employeeId,
            address);

        Assert.True(
            viewModel.HasAddress);

        Assert.Equal(
            "123 Nguyễn Trãi",
            viewModel.AddressLine);

        Assert.Equal(
            "Phường A",
            viewModel.Ward);

        Assert.Equal(
            "Quận B",
            viewModel.District);

        Assert.Equal(
            "Hà Nội",
            viewModel.Province);

        Assert.Equal(
            "Việt Nam",
            viewModel.Country);

        Assert.Equal(
            "100000",
            viewModel.PostalCode);

        Assert.True(
            viewModel.SaveCommand
                .CanExecute(null));

        Assert.True(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task AddressSlot_SaveCommand_WhenSuccessful_SendsRequestAndMarksAddressExisting()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubAddressService
            {
                SaveResult =
                    new EmployeeAddressOperationResult(
                        IsSuccessful: true)
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeAddressSlotViewModel(
                service,
                EmployeeAddressType.Permanent,
                "Địa chỉ thường trú",
                confirmationDialogService);

        viewModel.Load(
            employeeId,
            null);

        viewModel.AddressLine =
            "123 Lê Lợi";

        viewModel.Ward =
            "Phường 1";

        viewModel.District =
            "Quận 1";

        viewModel.Province =
            "TP. Hồ Chí Minh";

        viewModel.Country =
            "Việt Nam";

        viewModel.PostalCode =
            "700000";

        await viewModel.SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.SaveCallCount);

        Assert.NotNull(
            service.LastSaveRequest);

        Assert.Equal(
            employeeId,
            service.LastSaveRequest!.EmployeeId);

        Assert.Equal(
            EmployeeAddressType.Permanent,
            service.LastSaveRequest.Type);

        Assert.Equal(
            "123 Lê Lợi",
            service.LastSaveRequest.AddressLine);

        Assert.Equal(
            "Phường 1",
            service.LastSaveRequest.Ward);

        Assert.Equal(
            "Quận 1",
            service.LastSaveRequest.District);

        Assert.Equal(
            "TP. Hồ Chí Minh",
            service.LastSaveRequest.Province);

        Assert.Equal(
            "Việt Nam",
            service.LastSaveRequest.Country);

        Assert.Equal(
            "700000",
            service.LastSaveRequest.PostalCode);

        Assert.True(
            viewModel.HasAddress);

        Assert.Equal(
            "Đã lưu địa chỉ.",
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.True(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task AddressSlot_DeleteCommand_WhenSuccessful_ClearsSlot()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubAddressService
            {
                DeleteResult =
                    new EmployeeAddressOperationResult(
                        IsSuccessful: true)
            };

        var confirmationDialogService =
    new StubConfirmationDialogService();

        var viewModel =
            new EmployeeAddressSlotViewModel(
                service,
                EmployeeAddressType.Current,
                "Địa chỉ thường trú",
                confirmationDialogService);

        viewModel.Load(
            employeeId,
            new EmployeeAddressDetails(
                Guid.NewGuid(),
                EmployeeAddressType.Current,
                "Địa chỉ cần xóa",
                "Phường Test",
                "Quận Test",
                "Hà Nội",
                "Việt Nam",
                "100000"));

        await viewModel.DeleteCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.DeleteCallCount);

        Assert.Equal(
            employeeId,
            service.LastDeleteEmployeeId);

        Assert.Equal(
            EmployeeAddressType.Current,
            service.LastDeleteType);

        Assert.False(
            viewModel.HasAddress);

        Assert.Null(
            viewModel.AddressLine);

        Assert.Null(
            viewModel.Ward);

        Assert.Null(
            viewModel.District);

        Assert.Null(
            viewModel.Province);

        Assert.Equal(
            "Việt Nam",
            viewModel.Country);

        Assert.Null(
            viewModel.PostalCode);

        Assert.Equal(
            "Đã xóa địa chỉ.",
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task AddressSlot_SaveCommand_WhenServiceReturnsFailure_ShowsError()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubAddressService
            {
                SaveResult =
                    new EmployeeAddressOperationResult(
                        IsSuccessful: false,
                        ErrorMessage:
                            "Địa chỉ chi tiết là bắt buộc.")
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeAddressSlotViewModel(
                service,
                EmployeeAddressType.Current,
                "Địa chỉ thường trú",
                confirmationDialogService);

        viewModel.Load(
            employeeId,
            null);

        viewModel.AddressLine =
            string.Empty;

        await viewModel.SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.SaveCallCount);

        Assert.False(
            viewModel.HasAddress);

        Assert.Equal(
            "Địa chỉ chi tiết là bắt buộc.",
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task AddressSection_LoadAsync_MapsPermanentAndCurrentAddresses()
    {
        Guid employeeId =
            Guid.NewGuid();

        var permanent =
            new EmployeeAddressDetails(
                Guid.NewGuid(),
                EmployeeAddressType.Permanent,
                "111 Thường trú",
                null,
                null,
                "Hà Nội",
                "Việt Nam",
                null);

        var current =
            new EmployeeAddressDetails(
                Guid.NewGuid(),
                EmployeeAddressType.Current,
                "222 Hiện tại",
                null,
                null,
                "Đà Nẵng",
                "Việt Nam",
                null);

        var service =
            new StubAddressService
            {
                AddressBook =
                    new EmployeeAddressBookDetails(
                        employeeId,
                        permanent,
                        current)
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeAddressSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.IsLoading);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.True(
            viewModel.PermanentAddress.HasAddress);

        Assert.Equal(
            "111 Thường trú",
            viewModel.PermanentAddress.AddressLine);

        Assert.Equal(
            "Hà Nội",
            viewModel.PermanentAddress.Province);

        Assert.True(
            viewModel.CurrentAddress.HasAddress);

        Assert.Equal(
            "222 Hiện tại",
            viewModel.CurrentAddress.AddressLine);

        Assert.Equal(
            "Đà Nẵng",
            viewModel.CurrentAddress.Province);

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task AddressSection_LoadAsync_WhenServiceThrows_ShowsErrorAndClearsSlots()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubAddressService
            {
                ThrowWhenLoading =
                    true
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeAddressSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.IsLoading);

        Assert.Equal(
            "Không thể tải thông tin địa chỉ.",
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.PermanentAddress.HasAddress);

        Assert.False(
            viewModel.CurrentAddress.HasAddress);

        Assert.Null(
            viewModel.PermanentAddress.AddressLine);

        Assert.Null(
            viewModel.CurrentAddress.AddressLine);

        Assert.Equal(
            "Việt Nam",
            viewModel.PermanentAddress.Country);

        Assert.Equal(
            "Việt Nam",
            viewModel.CurrentAddress.Country);
    }

    [Fact]
    public async Task EmployeeProfile_LoadEmployeeAsync_LoadsPersonalAddressAndEmergencyContactSections()
    {
        var profileCompletionService =
            new StubEmployeeProfileCompletionService();

        Employee employee =
            CreateEmployee();

        var personalService =
            new StubPersonalProfileService
            {
                Details =
                    new EmployeePersonalProfileDetails(
                        employee.Id,
                        HasProfile: true,
                        PreferredName: "An",
                        Gender:
                            EmployeeGender.Male,
                        Nationality:
                            "Việt Nam",
                        PlaceOfBirth:
                            "Hà Nội")
            };

        var addressService =
            new StubAddressService
            {
                AddressBook =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress: null,
                        CurrentAddress: null)
            };

        var personalSection =
            new EmployeePersonalProfileSectionViewModel(
                personalService);

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var addressSection =
            new EmployeeAddressSectionViewModel(
                addressService,
                confirmationDialogService);

        var emergencyContactService =
            new StubEmergencyContactService();

        var emergencyContactSection =
            new EmployeeEmergencyContactSectionViewModel(
                emergencyContactService,
                confirmationDialogService);

        var identificationService =
            new StubIdentificationRecordService();

        var identificationSection =
            new EmployeeIdentificationRecordSectionViewModel(
                identificationService,
                confirmationDialogService);

        var viewModel =
            new EmployeeProfileViewModel(
                personalSection,
                addressSection,
                emergencyContactSection,
                identificationSection,
                profileCompletionService);

        await viewModel.LoadEmployeeAsync(
            employee);

        Assert.Equal(
            1,
            profileCompletionService.EvaluateCallCount);

        Assert.Equal(
            employee.Id,
            profileCompletionService.LastEmployeeId);

        Assert.Equal(
            employee.EmployeeCode,
            viewModel.EmployeeCode);

        Assert.Equal(
            employee.FullName,
            viewModel.FullName);

        Assert.Equal(
            employee.Department,
            viewModel.Department);

        Assert.Equal(
            employee.Position,
            viewModel.Position);

        Assert.Equal(
            "Đang làm việc",
            viewModel.StatusText);

        Assert.Equal(
            1,
            personalService.GetCallCount);

        Assert.Equal(
            1,
            addressService.GetCallCount);

        Assert.Equal(
            "An",
            viewModel
                .PersonalInformation
                .PreferredName);

        Assert.False(
            viewModel
                .Addresses
                .PermanentAddress
                .HasAddress);

        Assert.False(
            viewModel
                .Addresses
                .CurrentAddress
                .HasAddress);

        Assert.Equal(
            1,
            emergencyContactService.GetCallCount);

        Assert.Empty(
            viewModel.EmergencyContacts.Contacts);

        Assert.Equal(
            1,
            identificationService.GetCallCount);

        Assert.Empty(
            viewModel.IdentificationRecords.Records);

        Assert.Equal(
            1,
            profileCompletionService.EvaluateCallCount);

        Assert.Equal(
            employee.Id,
            profileCompletionService.LastEmployeeId);

        Assert.NotNull(
            viewModel.Completion);

        Assert.True(
            viewModel.IsProfileComplete);

        Assert.False(
            viewModel.RequiresProfileCompletion);

        Assert.False(
            viewModel.HasMissingProfileInformation);

        Assert.Equal(
            "Hồ sơ đã đầy đủ",
            viewModel.CompletionStatusText);

        Assert.Equal(
            string.Empty,
            viewModel.CompletionDetailsText);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-PROFILE-ADDRESS",
            "Nguyễn Văn An",
            "an@example.com",
            "0901234567",
            new DateOnly(
                1995,
                5,
                10),
            new DateOnly(
                2025,
                1,
                1),
            "Phòng Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);
    }

    private sealed class StubAddressService
        : IEmployeeAddressService
    {
        public EmployeeAddressBookDetails?
            AddressBook
        {
            get;
            set;
        }

        public EmployeeAddressOperationResult
            SaveResult
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true);

        public EmployeeAddressOperationResult
            DeleteResult
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true);

        public bool ThrowWhenLoading
        {
            get;
            set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public int SaveCallCount
        {
            get;
            private set;
        }

        public int DeleteCallCount
        {
            get;
            private set;
        }

        public SaveEmployeeAddressRequest?
            LastSaveRequest
        {
            get;
            private set;
        }

        public Guid?
            LastDeleteEmployeeId
        {
            get;
            private set;
        }

        public EmployeeAddressType?
            LastDeleteType
        {
            get;
            private set;
        }

        public Task<EmployeeAddressBookDetails>
            GetAddressesAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            if (ThrowWhenLoading)
            {
                return Task.FromException<
                    EmployeeAddressBookDetails>(
                    new InvalidOperationException(
                        "Test address load failure."));
            }

            return Task.FromResult(
                AddressBook
                ?? new EmployeeAddressBookDetails(
                    employeeId,
                    PermanentAddress: null,
                    CurrentAddress: null));
        }

        public Task<EmployeeAddressOperationResult>
            SaveAddressAsync(
                SaveEmployeeAddressRequest request,
                CancellationToken cancellationToken = default)
        {
            SaveCallCount++;

            LastSaveRequest =
                request;

            return Task.FromResult(
                SaveResult);
        }

        public Task<EmployeeAddressOperationResult>
            DeleteAddressAsync(
                Guid employeeId,
                EmployeeAddressType type,
                CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;

            LastDeleteEmployeeId =
                employeeId;

            LastDeleteType =
                type;

            return Task.FromResult(
                DeleteResult);
        }
    }

    private sealed class StubEmergencyContactService
    : IEmployeeEmergencyContactService
    {
        public int GetCallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<EmployeeEmergencyContactDetails>>
            GetContactsAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            IReadOnlyList<EmployeeEmergencyContactDetails> result =
                [];

            return Task.FromResult(
                result);
        }

        public Task<EmployeeEmergencyContactOperationResult>
            SaveContactAsync(
                SaveEmployeeEmergencyContactRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new EmployeeEmergencyContactOperationResult(
                    IsSuccessful: true,
                    ContactId:
                        request.ContactId
                        ?? Guid.NewGuid()));
        }

        public Task<EmployeeEmergencyContactOperationResult>
            DeleteContactAsync(
                Guid employeeId,
                Guid contactId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new EmployeeEmergencyContactOperationResult(
                    IsSuccessful: true));
        }
    }

    private sealed class StubPersonalProfileService
        : IEmployeePersonalProfileService
    {
        public EmployeePersonalProfileDetails?
            Details
        {
            get;
            set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public Task<EmployeePersonalProfileDetails>
            GetProfileAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            return Task.FromResult(
                Details
                ?? new EmployeePersonalProfileDetails(
                    employeeId,
                    HasProfile: false,
                    PreferredName: null,
                    Gender: null,
                    Nationality: null,
                    PlaceOfBirth: null));
        }

        public Task<SaveEmployeePersonalProfileResult>
            SaveProfileAsync(
                SaveEmployeePersonalProfileRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new SaveEmployeePersonalProfileResult(
                    IsSuccessful: true));
        }
    }

    private sealed class StubIdentificationRecordService
    : IEmployeeIdentificationRecordService
    {
        public int GetCallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<EmployeeIdentificationRecordDetails>>
            GetRecordsAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            IReadOnlyList<EmployeeIdentificationRecordDetails> result =
                [];

            return Task.FromResult(
                result);
        }

        public Task<EmployeeIdentificationRecordOperationResult>
            SaveRecordAsync(
                SaveEmployeeIdentificationRecordRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new EmployeeIdentificationRecordOperationResult(
                    IsSuccessful: true,
                    RecordId:
                        request.RecordId
                        ?? Guid.NewGuid()));
        }

        public Task<EmployeeIdentificationRecordOperationResult>
            DeleteRecordAsync(
                Guid employeeId,
                Guid recordId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new EmployeeIdentificationRecordOperationResult(
                    IsSuccessful: true));
        }
    }

    // Address — chọn No không được xóa
    [Fact]
    public async Task AddressSlot_DeleteCommand_WhenConfirmationDeclined_DoesNotDeleteOrChangeSlot()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubAddressService();

        var confirmationDialogService =
            new StubConfirmationDialogService
            {
                Result =
                    false
            };

        var viewModel =
            new EmployeeAddressSlotViewModel(
                service,
                EmployeeAddressType.Current,
                "Địa chỉ hiện tại",
                confirmationDialogService);

        viewModel.Load(
            employeeId,
            new EmployeeAddressDetails(
                Guid.NewGuid(),
                EmployeeAddressType.Current,
                "123 Nguyễn Trãi",
                "Phường A",
                "Quận B",
                "Hà Nội",
                "Việt Nam",
                "100000"));

        await viewModel.DeleteCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            confirmationDialogService.ConfirmCallCount);

        Assert.Equal(
            "Xác nhận xóa địa chỉ",
            confirmationDialogService.LastTitle);

        Assert.Equal(
            0,
            service.DeleteCallCount);

        Assert.True(
            viewModel.HasAddress);

        Assert.Equal(
            "123 Nguyễn Trãi",
            viewModel.AddressLine);

        Assert.Equal(
            "Phường A",
            viewModel.Ward);

        Assert.Equal(
            "Quận B",
            viewModel.District);

        Assert.Equal(
            "Hà Nội",
            viewModel.Province);

        Assert.Equal(
            "Việt Nam",
            viewModel.Country);

        Assert.Equal(
            "100000",
            viewModel.PostalCode);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    private sealed class StubEmployeeProfileCompletionService
    : IEmployeeProfileCompletionService
    {
        public int EvaluateCallCount
        {
            get;
            private set;
        }

        public Guid?
            LastEmployeeId
        {
            get;
            private set;
        }

        public EmployeeProfileCompletionResult
            Result
        {
            get;
            set;
        } =
            new(
                IsComplete: true,
                RequiresCompletion: false,
                MissingRequirements:
                    Array.Empty<
                        EmployeeProfileRequirement>());

        public Task<EmployeeProfileCompletionResult>
            EvaluateAsync(
                Employee employee,
                CancellationToken cancellationToken = default)
        {
            EvaluateCallCount++;

            LastEmployeeId =
                employee.Id;

            return Task.FromResult(
                Result);
        }
    }

    [Fact]
    public async Task EmployeeProfile_WhenAnySectionChanges_RefreshesCompletion()
    {
        Employee employee =
            CreateEmployee();

        var personalService =
            new StubPersonalProfileService();

        var addressService =
            new StubAddressService
            {
                AddressBook =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress: null,
                        CurrentAddress: null)
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var emergencyContactService =
            new StubEmergencyContactService();

        var identificationService =
            new StubIdentificationRecordService();

        var personalSection =
            new EmployeePersonalProfileSectionViewModel(
                personalService);

        var addressSection =
            new EmployeeAddressSectionViewModel(
                addressService,
                confirmationDialogService);

        var emergencyContactSection =
            new EmployeeEmergencyContactSectionViewModel(
                emergencyContactService,
                confirmationDialogService);

        var identificationSection =
            new EmployeeIdentificationRecordSectionViewModel(
                identificationService,
                confirmationDialogService);

        var completionService =
            new StubEmployeeProfileCompletionService
            {
                Result =
                    new EmployeeProfileCompletionResult(
                        IsComplete: false,
                        RequiresCompletion: true,
                        MissingRequirements:
                        [
                            EmployeeProfileRequirement.Gender,
                        EmployeeProfileRequirement.PermanentAddress,
                        EmployeeProfileRequirement.EmergencyContact,
                        EmployeeProfileRequirement.IdentificationRecord
                        ])
            };

        var viewModel =
            new EmployeeProfileViewModel(
                personalSection,
                addressSection,
                emergencyContactSection,
                identificationSection,
                completionService);

        await viewModel.LoadEmployeeAsync(
            employee);

        Assert.Equal(
            1,
            completionService.EvaluateCallCount);

        Assert.False(
            viewModel.IsProfileComplete);

        //
        // Personal Information
        //

        viewModel.PersonalInformation.SelectedGender =
            viewModel.PersonalInformation
                .GenderOptions
                .First();

        viewModel.PersonalInformation.Nationality =
            "Việt Nam";

        viewModel.PersonalInformation.PlaceOfBirth =
            "Hà Nội";

        await viewModel.PersonalInformation
            .SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            2,
            completionService.EvaluateCallCount);

        //
        // Permanent Address
        //

        viewModel.Addresses
            .PermanentAddress
            .AddressLine =
            "123 Nguyễn Trãi";

        await viewModel.Addresses
            .PermanentAddress
            .SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            3,
            completionService.EvaluateCallCount);

        //
        // Emergency Contact
        //

        viewModel.EmergencyContacts.FullName =
            "Nguyễn Văn Bình";

        viewModel.EmergencyContacts.Relationship =
            "Cha";

        viewModel.EmergencyContacts.PhoneNumber =
            "0901234567";

        await viewModel.EmergencyContacts
            .SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            4,
            completionService.EvaluateCallCount);

        //
        // Identification
        //

        completionService.Result =
            new EmployeeProfileCompletionResult(
                IsComplete: true,
                RequiresCompletion: false,
                MissingRequirements:
                    Array.Empty<EmployeeProfileRequirement>());

        viewModel.IdentificationRecords.DocumentNumber =
            "001234567890";

        await viewModel.IdentificationRecords
            .SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            5,
            completionService.EvaluateCallCount);

        Assert.True(
            viewModel.IsProfileComplete);

        Assert.False(
            viewModel.RequiresProfileCompletion);

        Assert.False(
            viewModel.HasMissingProfileInformation);

        Assert.Equal(
            "Hồ sơ đã đầy đủ",
            viewModel.CompletionStatusText);

        Assert.Equal(
            string.Empty,
            viewModel.CompletionDetailsText);
    }

    [Fact]
    public async Task EmployeeProfile_WhenAddressDeleteIsDeclined_DoesNotRefreshCompletion()
    {
        Employee employee =
            CreateEmployee();

        var permanentAddress =
            new EmployeeAddressDetails(
                Guid.NewGuid(),
                EmployeeAddressType.Permanent,
                "123 Nguyễn Trãi",
                null,
                null,
                "Hà Nội",
                "Việt Nam",
                null);

        var personalService =
            new StubPersonalProfileService();

        var addressService =
            new StubAddressService
            {
                AddressBook =
                    new EmployeeAddressBookDetails(
                        employee.Id,
                        PermanentAddress:
                            permanentAddress,
                        CurrentAddress:
                            null)
            };

        var confirmationDialogService =
            new StubConfirmationDialogService
            {
                Result =
                    false
            };

        var emergencyContactService =
            new StubEmergencyContactService();

        var identificationService =
            new StubIdentificationRecordService();

        var personalSection =
            new EmployeePersonalProfileSectionViewModel(
                personalService);

        var addressSection =
            new EmployeeAddressSectionViewModel(
                addressService,
                confirmationDialogService);

        var emergencyContactSection =
            new EmployeeEmergencyContactSectionViewModel(
                emergencyContactService,
                confirmationDialogService);

        var identificationSection =
            new EmployeeIdentificationRecordSectionViewModel(
                identificationService,
                confirmationDialogService);

        var completionService =
            new StubEmployeeProfileCompletionService
            {
                Result =
                    new EmployeeProfileCompletionResult(
                        IsComplete: false,
                        RequiresCompletion: true,
                        MissingRequirements:
                        [
                            EmployeeProfileRequirement.EmergencyContact
                        ])
            };

        var viewModel =
            new EmployeeProfileViewModel(
                personalSection,
                addressSection,
                emergencyContactSection,
                identificationSection,
                completionService);

        await viewModel.LoadEmployeeAsync(
            employee);

        Assert.Equal(
            1,
            completionService.EvaluateCallCount);

        Assert.True(
            viewModel.Addresses
                .PermanentAddress
                .HasAddress);

        await viewModel.Addresses
            .PermanentAddress
            .DeleteCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            confirmationDialogService.ConfirmCallCount);

        Assert.Equal(
            0,
            addressService.DeleteCallCount);

        Assert.Equal(
            1,
            completionService.EvaluateCallCount);

        Assert.True(
            viewModel.Addresses
                .PermanentAddress
                .HasAddress);

        Assert.Equal(
            "123 Nguyễn Trãi",
            viewModel.Addresses
                .PermanentAddress
                .AddressLine);
    }
}
