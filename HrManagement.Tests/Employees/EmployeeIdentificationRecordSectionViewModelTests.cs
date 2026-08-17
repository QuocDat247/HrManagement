using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeIdentificationRecordSectionViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenRecordsExist_LoadsAndSelectsFirstRecord()
    {
        Guid employeeId =
            Guid.NewGuid();

        var first =
            new EmployeeIdentificationRecordDetails(
                Guid.NewGuid(),
                EmployeeIdentificationType.NationalId,
                "012345678901",
                new DateOnly(
                    2024,
                    1,
                    10),
                new DateOnly(
                    2034,
                    1,
                    10),
                "Cơ quan A",
                "Hà Nội",
                "Việt Nam");

        var second =
            new EmployeeIdentificationRecordDetails(
                Guid.NewGuid(),
                EmployeeIdentificationType.Passport,
                "P1234567",
                null,
                null,
                null,
                null,
                null);

        var service =
            new StubIdentificationRecordService
            {
                RecordsToReturn =
                    new[]
                    {
                        first,
                        second
                    }
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.IsBusy);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Equal(
            2,
            viewModel.Records.Count);

        Assert.Same(
            first,
            viewModel.SelectedRecord);

        Assert.Equal(
            first.Id,
            viewModel.EditingRecordId);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            viewModel.SelectedType!.Value);

        Assert.Equal(
            "012345678901",
            viewModel.DocumentNumber);

        Assert.Equal(
            new DateTime(
                2024,
                1,
                10),
            viewModel.IssueDate);

        Assert.Equal(
            new DateTime(
                2034,
                1,
                10),
            viewModel.ExpiryDate);

        Assert.Equal(
            "Cơ quan A",
            viewModel.IssuingAuthority);

        Assert.Equal(
            "Hà Nội",
            viewModel.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            viewModel.IssuingCountry);

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
            new StubIdentificationRecordService
            {
                ThrowWhenLoading =
                    true
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.IsBusy);

        Assert.Empty(
            viewModel.Records);

        Assert.Null(
            viewModel.SelectedRecord);

        Assert.Null(
            viewModel.EditingRecordId);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            viewModel.SelectedType!.Value);

        Assert.Null(
            viewModel.DocumentNumber);

        Assert.Null(
            viewModel.IssueDate);

        Assert.Null(
            viewModel.ExpiryDate);

        Assert.Null(
            viewModel.IssuingAuthority);

        Assert.Null(
            viewModel.PlaceOfIssue);

        Assert.Null(
            viewModel.IssuingCountry);

        Assert.Equal(
            "Không thể tải thông tin giấy tờ định danh.",
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task SelectedRecord_WhenChanged_MapsRecordIntoEditor()
    {
        Guid employeeId =
            Guid.NewGuid();

        var first =
            new EmployeeIdentificationRecordDetails(
                Guid.NewGuid(),
                EmployeeIdentificationType.NationalId,
                "NATIONAL-001",
                null,
                null,
                null,
                null,
                null);

        var second =
            new EmployeeIdentificationRecordDetails(
                Guid.NewGuid(),
                EmployeeIdentificationType.Passport,
                "P1234567",
                new DateOnly(
                    2025,
                    2,
                    1),
                new DateOnly(
                    2035,
                    2,
                    1),
                "Cục Quản lý xuất nhập cảnh",
                "TP. Hồ Chí Minh",
                "Việt Nam");

        var service =
            new StubIdentificationRecordService
            {
                RecordsToReturn =
                    new[]
                    {
                        first,
                        second
                    }
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.SelectedRecord =
            second;

        Assert.Equal(
            second.Id,
            viewModel.EditingRecordId);

        Assert.Equal(
            EmployeeIdentificationType.Passport,
            viewModel.SelectedType!.Value);

        Assert.Equal(
            "P1234567",
            viewModel.DocumentNumber);

        Assert.Equal(
            new DateTime(
                2025,
                2,
                1),
            viewModel.IssueDate);

        Assert.Equal(
            new DateTime(
                2035,
                2,
                1),
            viewModel.ExpiryDate);

        Assert.Equal(
            "Cục Quản lý xuất nhập cảnh",
            viewModel.IssuingAuthority);

        Assert.Equal(
            "TP. Hồ Chí Minh",
            viewModel.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            viewModel.IssuingCountry);

        Assert.True(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task NewCommand_ClearsCurrentEditorAndDefaultsToNationalId()
    {
        Guid employeeId =
            Guid.NewGuid();

        var existing =
            new EmployeeIdentificationRecordDetails(
                Guid.NewGuid(),
                EmployeeIdentificationType.Passport,
                "P1234567",
                new DateOnly(
                    2025,
                    1,
                    1),
                new DateOnly(
                    2035,
                    1,
                    1),
                "Cơ quan cấp",
                "Hà Nội",
                "Việt Nam");

        var service =
            new StubIdentificationRecordService
            {
                RecordsToReturn =
                    new[]
                    {
                        existing
                    }
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        Assert.True(
            viewModel.NewCommand
                .CanExecute(null));

        viewModel.NewCommand
            .Execute(null);

        Assert.Null(
            viewModel.SelectedRecord);

        Assert.Null(
            viewModel.EditingRecordId);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            viewModel.SelectedType!.Value);

        Assert.Null(
            viewModel.DocumentNumber);

        Assert.Null(
            viewModel.IssueDate);

        Assert.Null(
            viewModel.ExpiryDate);

        Assert.Null(
            viewModel.IssuingAuthority);

        Assert.Null(
            viewModel.PlaceOfIssue);

        Assert.Null(
            viewModel.IssuingCountry);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNewRecordSucceeds_RefreshesAndSelectsSavedRecord()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid savedRecordId =
            Guid.NewGuid();

        var savedDetails =
            new EmployeeIdentificationRecordDetails(
                savedRecordId,
                EmployeeIdentificationType.Passport,
                "P1234567",
                new DateOnly(
                    2025,
                    1,
                    10),
                new DateOnly(
                    2035,
                    1,
                    10),
                "Cục Quản lý xuất nhập cảnh",
                "Hà Nội",
                "Việt Nam");

        var service =
            new StubIdentificationRecordService
            {
                RecordsToReturn =
                    Array.Empty<
                        EmployeeIdentificationRecordDetails>(),

                SaveResult =
                    new EmployeeIdentificationRecordOperationResult(
                        IsSuccessful: true,
                        RecordId:
                            savedRecordId)
            };

        service.OnSave =
            _ =>
            {
                service.RecordsToReturn =
                    new[]
                    {
                        savedDetails
                    };
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.SelectedType =
            viewModel.TypeOptions.Single(
                option =>
                    option.Value ==
                    EmployeeIdentificationType.Passport);

        viewModel.DocumentNumber =
            "P1234567";

        viewModel.IssueDate =
            new DateTime(
                2025,
                1,
                10);

        viewModel.ExpiryDate =
            new DateTime(
                2035,
                1,
                10);

        viewModel.IssuingAuthority =
            "Cục Quản lý xuất nhập cảnh";

        viewModel.PlaceOfIssue =
            "Hà Nội";

        viewModel.IssuingCountry =
            "Việt Nam";

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
            service.LastSaveRequest.RecordId);

        Assert.Equal(
            EmployeeIdentificationType.Passport,
            service.LastSaveRequest.Type);

        Assert.Equal(
            "P1234567",
            service.LastSaveRequest.DocumentNumber);

        Assert.Equal(
            new DateOnly(
                2025,
                1,
                10),
            service.LastSaveRequest.IssueDate);

        Assert.Equal(
            new DateOnly(
                2035,
                1,
                10),
            service.LastSaveRequest.ExpiryDate);

        Assert.Equal(
            "Cục Quản lý xuất nhập cảnh",
            service.LastSaveRequest.IssuingAuthority);

        Assert.Equal(
            "Hà Nội",
            service.LastSaveRequest.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            service.LastSaveRequest.IssuingCountry);

        Assert.Equal(
            2,
            service.GetCallCount);

        EmployeeIdentificationRecordDetails selected =
            Assert.Single(
                viewModel.Records);

        Assert.Equal(
            savedRecordId,
            selected.Id);

        Assert.Same(
            selected,
            viewModel.SelectedRecord);

        Assert.Equal(
            savedRecordId,
            viewModel.EditingRecordId);

        Assert.Equal(
            "Đã lưu giấy tờ định danh.",
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
            new StubIdentificationRecordService
            {
                SaveResult =
                    new EmployeeIdentificationRecordOperationResult(
                        IsSuccessful: false,
                        ErrorMessage:
                            "Số giấy tờ là bắt buộc.")
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.DocumentNumber =
            string.Empty;

        await viewModel.SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.SaveCallCount);

        Assert.Equal(
            1,
            service.GetCallCount);

        Assert.Equal(
            "Số giấy tờ là bắt buộc.",
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.Empty(
            viewModel.Records);

        Assert.Null(
            viewModel.EditingRecordId);
    }

    [Fact]
    public async Task DeleteCommand_WhenSuccessful_RefreshesAndClearsEditor()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid recordId =
            Guid.NewGuid();

        var existing =
            new EmployeeIdentificationRecordDetails(
                recordId,
                EmployeeIdentificationType.NationalId,
                "012345678901",
                null,
                null,
                null,
                null,
                null);

        var service =
            new StubIdentificationRecordService
            {
                RecordsToReturn =
                    new[]
                    {
                        existing
                    },

                DeleteResult =
                    new EmployeeIdentificationRecordOperationResult(
                        IsSuccessful: true)
            };

        service.OnDelete =
            (_, _) =>
            {
                service.RecordsToReturn =
                    Array.Empty<
                        EmployeeIdentificationRecordDetails>();
            };

        var viewModel =
            new EmployeeIdentificationRecordSectionViewModel(
                service);

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
            recordId,
            service.LastDeletedRecordId);

        Assert.Equal(
            2,
            service.GetCallCount);

        Assert.Empty(
            viewModel.Records);

        Assert.Null(
            viewModel.SelectedRecord);

        Assert.Null(
            viewModel.EditingRecordId);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            viewModel.SelectedType!.Value);

        Assert.Null(
            viewModel.DocumentNumber);

        Assert.Null(
            viewModel.IssueDate);

        Assert.Null(
            viewModel.ExpiryDate);

        Assert.Null(
            viewModel.IssuingAuthority);

        Assert.Null(
            viewModel.PlaceOfIssue);

        Assert.Null(
            viewModel.IssuingCountry);

        Assert.Equal(
            "Đã xóa giấy tờ định danh.",
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.DeleteCommand
                .CanExecute(null));
    }

    private sealed class StubIdentificationRecordService
        : IEmployeeIdentificationRecordService
    {
        public IReadOnlyList<EmployeeIdentificationRecordDetails>
            RecordsToReturn
        {
            get;
            set;
        } =
            Array.Empty<
                EmployeeIdentificationRecordDetails>();

        public EmployeeIdentificationRecordOperationResult
            SaveResult
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true);

        public EmployeeIdentificationRecordOperationResult
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

        public SaveEmployeeIdentificationRecordRequest?
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
            LastDeletedRecordId
        {
            get;
            private set;
        }

        public Action<SaveEmployeeIdentificationRecordRequest>?
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

        public Task<IReadOnlyList<EmployeeIdentificationRecordDetails>>
            GetRecordsAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            if (ThrowWhenLoading)
            {
                return Task.FromException<
                    IReadOnlyList<
                        EmployeeIdentificationRecordDetails>>(
                    new InvalidOperationException(
                        "Test identification load failure."));
            }

            return Task.FromResult(
                RecordsToReturn);
        }

        public Task<EmployeeIdentificationRecordOperationResult>
            SaveRecordAsync(
                SaveEmployeeIdentificationRecordRequest request,
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

        public Task<EmployeeIdentificationRecordOperationResult>
            DeleteRecordAsync(
                Guid employeeId,
                Guid recordId,
                CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;

            LastDeletedEmployeeId =
                employeeId;

            LastDeletedRecordId =
                recordId;

            OnDelete?.Invoke(
                employeeId,
                recordId);

            return Task.FromResult(
                DeleteResult);
        }
    }
}
