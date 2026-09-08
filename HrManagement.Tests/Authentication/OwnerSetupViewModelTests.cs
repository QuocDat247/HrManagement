using HrManagement.Application.Authentication.Bootstrap;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Authentication;

public sealed class OwnerSetupViewModelTests
{
    [Fact]
    public async Task
        CreateOwnerCommand_WithValidValues_RaisesOwnerCreated()
    {
        var service =
            new TestBootstrapService();

        var viewModel =
            new OwnerSetupViewModel(
                service)
            {
                Username =
                    "owner",

                DisplayName =
                    "Chủ doanh nghiệp"
            };

        bool ownerCreated =
            false;

        viewModel.OwnerCreated +=
            (_, _) =>
                ownerCreated = true;

        await viewModel.CreateOwnerCommand
            .ExecuteAsync(
                new OwnerSetupPasswords(
                    "A secure owner password 2026",
                    "A secure owner password 2026"));

        Assert.True(
            service.CreateCalled);

        Assert.Equal(
            "owner",
            service.Username);

        Assert.Equal(
            "Chủ doanh nghiệp",
            service.DisplayName);

        Assert.Equal(
            "A secure owner password 2026",
            service.Password);

        Assert.True(
            ownerCreated);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task
        CreateOwnerCommand_WithMismatchedConfirmation_DoesNotCallService()
    {
        var service =
            new TestBootstrapService();

        var viewModel =
            new OwnerSetupViewModel(
                service)
            {
                Username =
                    "owner",

                DisplayName =
                    "Chủ doanh nghiệp"
            };

        await viewModel.CreateOwnerCommand
            .ExecuteAsync(
                new OwnerSetupPasswords(
                    "A secure owner password 2026",
                    "Different confirmation password"));

        Assert.False(
            service.CreateCalled);

        Assert.Equal(
            "Mật khẩu xác nhận không khớp.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task
        CreateOwnerCommand_WhenBootstrapFails_ShowsServiceError()
    {
        var service =
            new TestBootstrapService
            {
                Result =
                    new OwnerBootstrapResult(
                        false,
                        "Mật khẩu này không được phép sử dụng.")
            };

        var viewModel =
            new OwnerSetupViewModel(
                service)
            {
                Username =
                    "owner",

                DisplayName =
                    "Chủ doanh nghiệp"
            };

        await viewModel.CreateOwnerCommand
            .ExecuteAsync(
                new OwnerSetupPasswords(
                    "A secure owner password 2026",
                    "A secure owner password 2026"));

        Assert.True(
            service.CreateCalled);

        Assert.Equal(
            "Mật khẩu này không được phép sử dụng.",
            viewModel.ErrorMessage);
    }

    private sealed class TestBootstrapService
        : IInitialOwnerBootstrapService
    {
        public OwnerBootstrapResult Result
        {
            get;
            set;
        } =
            new(
                true);

        public bool CreateCalled
        {
            get;
            private set;
        }

        public string? Username
        {
            get;
            private set;
        }

        public string? DisplayName
        {
            get;
            private set;
        }

        public string? Password
        {
            get;
            private set;
        }

        public Task<bool> IsBootstrapRequiredAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                true);
        }

        public Task<OwnerBootstrapResult>
            CreateInitialOwnerAsync(
                string username,
                string displayName,
                string password,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CreateCalled =
                true;

            Username =
                username;

            DisplayName =
                displayName;

            Password =
                password;

            return Task.FromResult(
                Result);
        }
    }
}
