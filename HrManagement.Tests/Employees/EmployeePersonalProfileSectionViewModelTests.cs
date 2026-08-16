using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeePersonalProfileSectionViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenProfileExists_LoadsValues()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubPersonalProfileService
            {
                DetailsToReturn =
                    new EmployeePersonalProfileDetails(
                        employeeId,
                        HasProfile: true,
                        PreferredName: "An",
                        Gender: EmployeeGender.Male,
                        Nationality: "Việt Nam",
                        PlaceOfBirth: "Hà Nội")
            };

        var viewModel =
            new EmployeePersonalProfileSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        Assert.True(
            viewModel.HasProfile);

        Assert.Equal(
            "An",
            viewModel.PreferredName);

        Assert.NotNull(
            viewModel.SelectedGender);

        Assert.Equal(
            EmployeeGender.Male,
            viewModel.SelectedGender!.Value);

        Assert.Equal(
            "Nam",
            viewModel.SelectedGender.DisplayName);

        Assert.Equal(
            "Việt Nam",
            viewModel.Nationality);

        Assert.Equal(
            "Hà Nội",
            viewModel.PlaceOfBirth);

        Assert.False(
            viewModel.IsLoading);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.True(
            viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoadAsync_WhenProfileDoesNotExist_LoadsEmptyState()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubPersonalProfileService
            {
                DetailsToReturn =
                    new EmployeePersonalProfileDetails(
                        employeeId,
                        HasProfile: false,
                        PreferredName: null,
                        Gender: null,
                        Nationality: null,
                        PlaceOfBirth: null)
            };

        var viewModel =
            new EmployeePersonalProfileSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.HasProfile);

        Assert.Null(
            viewModel.PreferredName);

        Assert.Null(
            viewModel.SelectedGender);

        Assert.Null(
            viewModel.Nationality);

        Assert.Null(
            viewModel.PlaceOfBirth);

        Assert.False(
            viewModel.IsLoading);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.True(
            viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenValid_SavesProfileAndShowsSuccess()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubPersonalProfileService
            {
                DetailsToReturn =
                    new EmployeePersonalProfileDetails(
                        employeeId,
                        HasProfile: false,
                        PreferredName: null,
                        Gender: null,
                        Nationality: null,
                        PlaceOfBirth: null),

                SaveResultToReturn =
                    new SaveEmployeePersonalProfileResult(
                        IsSuccessful: true)
            };

        var viewModel =
            new EmployeePersonalProfileSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        viewModel.PreferredName =
            "An";

        viewModel.SelectedGender =
            viewModel.GenderOptions.Single(
                option =>
                    option.Value ==
                    EmployeeGender.Male);

        viewModel.Nationality =
            "Việt Nam";

        viewModel.PlaceOfBirth =
            "Hà Nội";

        await viewModel.SaveCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.SaveCallCount);

        Assert.NotNull(
            service.RequestPassedToSave);

        Assert.Equal(
            employeeId,
            service.RequestPassedToSave!.EmployeeId);

        Assert.Equal(
            "An",
            service.RequestPassedToSave.PreferredName);

        Assert.Equal(
            EmployeeGender.Male,
            service.RequestPassedToSave.Gender);

        Assert.Equal(
            "Việt Nam",
            service.RequestPassedToSave.Nationality);

        Assert.Equal(
            "Hà Nội",
            service.RequestPassedToSave.PlaceOfBirth);

        Assert.True(
            viewModel.HasProfile);

        Assert.Equal(
            "Đã lưu thông tin cá nhân.",
            viewModel.SuccessMessage);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsSaving);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceThrows_ShowsErrorState()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubPersonalProfileService
            {
                ThrowWhenLoading =
                    true
            };

        var viewModel =
            new EmployeePersonalProfileSectionViewModel(
                service);

        await viewModel.LoadAsync(
            employeeId);

        Assert.False(
            viewModel.HasProfile);

        Assert.Null(
            viewModel.PreferredName);

        Assert.Null(
            viewModel.SelectedGender);

        Assert.Null(
            viewModel.Nationality);

        Assert.Null(
            viewModel.PlaceOfBirth);

        Assert.False(
            viewModel.IsLoading);

        Assert.Equal(
            "Không thể tải thông tin cá nhân mở rộng.",
            viewModel.ErrorMessage);

        Assert.Null(
            viewModel.SuccessMessage);

        Assert.True(
            viewModel.SaveCommand.CanExecute(null));
    }

    private sealed class StubPersonalProfileService
        : IEmployeePersonalProfileService
    {
        public EmployeePersonalProfileDetails?
            DetailsToReturn
        {
            get;
            set;
        }

        public SaveEmployeePersonalProfileResult
            SaveResultToReturn
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

        public int SaveCallCount
        {
            get;
            private set;
        }

        public SaveEmployeePersonalProfileRequest?
            RequestPassedToSave
        {
            get;
            private set;
        }

        public Task<EmployeePersonalProfileDetails>
            GetProfileAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            if (ThrowWhenLoading)
            {
                return Task.FromException<
                    EmployeePersonalProfileDetails>(
                    new InvalidOperationException(
                        "Test load failure."));
            }

            return Task.FromResult(
                DetailsToReturn
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
            SaveCallCount++;

            RequestPassedToSave =
                request;

            return Task.FromResult(
                SaveResultToReturn);
        }
    }
}
