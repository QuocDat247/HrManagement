using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;
using HrManagement.Tests.TestDoubles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeEmergencyContactSectionViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenContactsExist_LoadsAndSelectsFirstContact()
    {
        Guid employeeId =
            Guid.NewGuid();

        var first =
            new EmployeeEmergencyContactDetails(
                Guid.NewGuid(),
                "Nguyễn Văn Bình",
                "Cha",
                "0901000001",
                "binh@example.com",
                IsPrimary: true);

        var second =
            new EmployeeEmergencyContactDetails(
                Guid.NewGuid(),
                "Nguyễn Thị Hoa",
                "Mẹ",
                "0901000002",
                null,
                IsPrimary: false);

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    new[]
                    {
                        first,
                        second
                    }
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.IsBusy);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Equal(
            2,
            viewModel.Contacts.Count);

        Assert.Same(
            first,
            viewModel.SelectedContact);

        Assert.Equal(
            first.Id,
            viewModel.EditingContactId);

        Assert.Equal(
            "Nguyễn Văn Bình",
            viewModel.FullName);

        Assert.Equal(
            "Cha",
            viewModel.Relationship);

        Assert.Equal(
            "0901000001",
            viewModel.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            viewModel.Email);

        Assert.True(
            viewModel.IsPrimary);

        Assert.True(
            viewModel.DeleteCommand
                .CanExecute(null));

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceThrows_ShowsErrorAndClearsEditor()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubEmergencyContactService
            {
                ThrowWhenLoading =
                    true
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.IsBusy);

        Assert.Empty(
            viewModel.Contacts);

        Assert.Null(
            viewModel.SelectedContact);

        Assert.Null(
            viewModel.EditingContactId);

        Assert.Null(
            viewModel.FullName);

        Assert.Null(
            viewModel.Relationship);

        Assert.Null(
            viewModel.PhoneNumber);

        Assert.Null(
            viewModel.Email);

        Assert.False(
            viewModel.IsPrimary);

        Assert.Equal(
            "Không thể tải thông tin liên hệ khẩn cấp.",
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task SelectedContact_WhenChanged_MapsContactIntoEditor()
    {
        Guid employeeId =
            Guid.NewGuid();

        var first =
            new EmployeeEmergencyContactDetails(
                Guid.NewGuid(),
                "Người thứ nhất",
                "Cha",
                "0901000001",
                null,
                IsPrimary: true);

        var second =
            new EmployeeEmergencyContactDetails(
                Guid.NewGuid(),
                "Người thứ hai",
                "Anh trai",
                "0901000002",
                "second@example.com",
                IsPrimary: false);

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    new[]
                    {
                        first,
                        second
                    }
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.SelectedContact =
            second;

        Assert.Equal(
            second.Id,
            viewModel.EditingContactId);

        Assert.Equal(
            "Người thứ hai",
            viewModel.FullName);

        Assert.Equal(
            "Anh trai",
            viewModel.Relationship);

        Assert.Equal(
            "0901000002",
            viewModel.PhoneNumber);

        Assert.Equal(
            "second@example.com",
            viewModel.Email);

        Assert.False(
            viewModel.IsPrimary);

        Assert.True(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task NewCommand_ClearsCurrentEditorForNewContact()
    {
        Guid employeeId =
            Guid.NewGuid();

        var existing =
            new EmployeeEmergencyContactDetails(
                Guid.NewGuid(),
                "Nguyễn Văn Bình",
                "Cha",
                "0901000001",
                "binh@example.com",
                IsPrimary: true);

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    new[]
                    {
                        existing
                    }
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        Assert.True(
            viewModel.NewCommand
                .CanExecute(null));

        viewModel.NewCommand
            .Execute(null);

        Assert.Null(
            viewModel.SelectedContact);

        Assert.Null(
            viewModel.EditingContactId);

        Assert.Null(
            viewModel.FullName);

        Assert.Null(
            viewModel.Relationship);

        Assert.Null(
            viewModel.PhoneNumber);

        Assert.Null(
            viewModel.Email);

        Assert.False(
            viewModel.IsPrimary);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNewContactSucceeds_RefreshesAndSelectsSavedContact()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid savedContactId =
            Guid.NewGuid();

        var savedDetails =
            new EmployeeEmergencyContactDetails(
                savedContactId,
                "Nguyễn Văn Bình",
                "Cha",
                "+84 901 234 567",
                "binh@example.com",
                IsPrimary: true);

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    Array.Empty<
                        EmployeeEmergencyContactDetails>(),

                SaveResult =
                    new EmployeeEmergencyContactOperationResult(
                        IsSuccessful: true,
                        ContactId:
                            savedContactId)
            };

        service.OnSave =
            _ =>
            {
                service.ContactsToReturn =
                    new[]
                    {
                        savedDetails
                    };
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.FullName =
            "Nguyễn Văn Bình";

        viewModel.Relationship =
            "Cha";

        viewModel.PhoneNumber =
            "+84 901 234 567";

        viewModel.Email =
            "binh@example.com";

        viewModel.IsPrimary =
            true;

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

        Assert.Null(
            service.LastSaveRequest.ContactId);

        Assert.Equal(
            "Nguyễn Văn Bình",
            service.LastSaveRequest.FullName);

        Assert.Equal(
            "Cha",
            service.LastSaveRequest.Relationship);

        Assert.Equal(
            "+84 901 234 567",
            service.LastSaveRequest.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            service.LastSaveRequest.Email);

        Assert.True(
            service.LastSaveRequest.IsPrimary);

        Assert.Equal(
            2,
            service.GetCallCount);

        EmployeeEmergencyContactDetails selected =
            Assert.Single(
                viewModel.Contacts);

        Assert.Equal(
            savedContactId,
            selected.Id);

        Assert.Same(
            selected,
            viewModel.SelectedContact);

        Assert.Equal(
            savedContactId,
            viewModel.EditingContactId);

        Assert.True(
            viewModel.IsPrimary);

        Assert.Equal(
            "Đã lưu liên hệ khẩn cấp.",
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.True(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenServiceReturnsFailure_ShowsErrorWithoutRefreshing()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    Array.Empty<
                        EmployeeEmergencyContactDetails>(),

                SaveResult =
                    new EmployeeEmergencyContactOperationResult(
                        IsSuccessful: false,
                        ErrorMessage:
                            "Họ tên người liên hệ là bắt buộc.")
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.FullName =
            string.Empty;

        viewModel.Relationship =
            "Cha";

        viewModel.PhoneNumber =
            "0901234567";

        await viewModel.SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.SaveCallCount);

        Assert.Equal(
            1,
            service.GetCallCount);

        Assert.Equal(
            "Họ tên người liên hệ là bắt buộc.",
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.Empty(
            viewModel.Contacts);

        Assert.Null(
            viewModel.EditingContactId);
    }

    [Fact]
    public async Task DeleteCommand_WhenSuccessful_RefreshesAndClearsEditor()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid contactId =
            Guid.NewGuid();

        var existing =
            new EmployeeEmergencyContactDetails(
                contactId,
                "Nguyễn Văn Bình",
                "Cha",
                "0901000001",
                null,
                IsPrimary: true);

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    new[]
                    {
                        existing
                    },

                DeleteResult =
                    new EmployeeEmergencyContactOperationResult(
                        IsSuccessful: true)
            };

        service.OnDelete =
            (_, _) =>
            {
                service.ContactsToReturn =
                    Array.Empty<
                        EmployeeEmergencyContactDetails>();
            };

        var confirmationDialogService =
            new StubConfirmationDialogService();

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        await viewModel.DeleteCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.DeleteCallCount);

        Assert.Equal(
            employeeId,
            service.LastDeletedEmployeeId);

        Assert.Equal(
            contactId,
            service.LastDeletedContactId);

        Assert.Equal(
            2,
            service.GetCallCount);

        Assert.Empty(
            viewModel.Contacts);

        Assert.Null(
            viewModel.SelectedContact);

        Assert.Null(
            viewModel.EditingContactId);

        Assert.Null(
            viewModel.FullName);

        Assert.Null(
            viewModel.Relationship);

        Assert.Null(
            viewModel.PhoneNumber);

        Assert.Null(
            viewModel.Email);

        Assert.False(
            viewModel.IsPrimary);

        Assert.Equal(
            "Đã xóa liên hệ khẩn cấp.",
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    private sealed class StubEmergencyContactService
        : IEmployeeEmergencyContactService
    {
        public IReadOnlyList<EmployeeEmergencyContactDetails>
            ContactsToReturn
        {
            get;
            set;
        } =
            Array.Empty<
                EmployeeEmergencyContactDetails>();

        public EmployeeEmergencyContactOperationResult
            SaveResult
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true);

        public EmployeeEmergencyContactOperationResult
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

        public SaveEmployeeEmergencyContactRequest?
            LastSaveRequest
        {
            get;
            private set;
        }

        public Guid?
            LastDeletedEmployeeId
        {
            get;
            private set;
        }

        public Guid?
            LastDeletedContactId
        {
            get;
            private set;
        }

        public Action<SaveEmployeeEmergencyContactRequest>?
            OnSave
        {
            get;
            set;
        }

        public Action<Guid, Guid>?
            OnDelete
        {
            get;
            set;
        }

        public Task<IReadOnlyList<EmployeeEmergencyContactDetails>>
            GetContactsAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            if (ThrowWhenLoading)
            {
                return Task.FromException<
                    IReadOnlyList<
                        EmployeeEmergencyContactDetails>>(
                    new InvalidOperationException(
                        "Test emergency contact load failure."));
            }

            return Task.FromResult(
                ContactsToReturn);
        }

        public Task<EmployeeEmergencyContactOperationResult>
            SaveContactAsync(
                SaveEmployeeEmergencyContactRequest request,
                CancellationToken cancellationToken = default)
        {
            SaveCallCount++;

            LastSaveRequest =
                request;

            OnSave?.Invoke(
                request);

            return Task.FromResult(
                SaveResult);
        }

        public Task<EmployeeEmergencyContactOperationResult>
            DeleteContactAsync(
                Guid employeeId,
                Guid contactId,
                CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;

            LastDeletedEmployeeId =
                employeeId;

            LastDeletedContactId =
                contactId;

            OnDelete?.Invoke(
                employeeId,
                contactId);

            return Task.FromResult(
                DeleteResult);
        }
    }

    // Emergency Contact — chọn No giữ nguyên selection/editor
    [Fact]
    public async Task DeleteCommand_WhenConfirmationDeclined_DoesNotDeleteOrRefresh()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid contactId =
            Guid.NewGuid();

        var existing =
            new EmployeeEmergencyContactDetails(
                contactId,
                "Nguyễn Văn Bình",
                "Cha",
                "0901000001",
                "binh@example.com",
                IsPrimary: true);

        var service =
            new StubEmergencyContactService
            {
                ContactsToReturn =
                    new[]
                    {
                    existing
                    }
            };

        var confirmationDialogService =
            new StubConfirmationDialogService
            {
                Result =
                    false
            };

        var viewModel =
            new EmployeeEmergencyContactSectionViewModel(
                service,
                confirmationDialogService);

        await viewModel.LoadAsync(
            employeeId);

        await viewModel.DeleteCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            confirmationDialogService.ConfirmCallCount);

        Assert.Equal(
            "Xác nhận xóa liên hệ",
            confirmationDialogService.LastTitle);

        Assert.Contains(
            "Nguyễn Văn Bình",
            confirmationDialogService.LastMessage);

        Assert.Equal(
            0,
            service.DeleteCallCount);

        // Không refresh sau khi Cancel.
        Assert.Equal(
            1,
            service.GetCallCount);

        EmployeeEmergencyContactDetails remaining =
            Assert.Single(
                viewModel.Contacts);

        Assert.Same(
            existing,
            remaining);

        Assert.Same(
            existing,
            viewModel.SelectedContact);

        Assert.Equal(
            contactId,
            viewModel.EditingContactId);

        Assert.Equal(
            "Nguyễn Văn Bình",
            viewModel.FullName);

        Assert.Equal(
            "Cha",
            viewModel.Relationship);

        Assert.Equal(
            "0901000001",
            viewModel.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            viewModel.Email);

        Assert.True(
            viewModel.IsPrimary);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);
    }
}
