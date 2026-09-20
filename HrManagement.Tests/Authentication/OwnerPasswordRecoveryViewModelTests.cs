using HrManagement.Application.Authentication.Recovery;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Authentication;

public sealed class OwnerPasswordRecoveryViewModelTests
{
    [Fact]
    public async Task
        RecoverCommand_WithMismatchedPasswords_DoesNotCallService()
    {
        var service =
            new TestRecoveryService();

        var viewModel =
            new OwnerPasswordRecoveryViewModel(
                service)
            {
                Username =
                    "owner"
            };

        await viewModel.RecoverCommand
            .ExecuteAsync(
                new OwnerPasswordRecoveryPasswords(
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026",
                    "Different password"));

        Assert.False(
            service.Called);

        Assert.Equal(
            "Mật khẩu xác nhận không khớp.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task
        RecoverCommand_WhenSuccessful_ExposesNewRecoveryCode()
    {
        var service =
            new TestRecoveryService();

        var viewModel =
            new OwnerPasswordRecoveryViewModel(
                service)
            {
                Username =
                    "owner"
            };

        bool recoverySucceeded =
            false;

        viewModel.RecoverySucceeded +=
            (_, _) =>
                recoverySucceeded = true;

        await viewModel.RecoverCommand
            .ExecuteAsync(
                new OwnerPasswordRecoveryPasswords(
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026",
                    "A new secure owner password 2026"));

        Assert.True(
            service.Called);

        Assert.True(
            viewModel.IsRecoveryCompleted);

        Assert.True(
            recoverySucceeded);

        Assert.False(
            viewModel.IsCompletionAcknowledged);

        Assert.Equal(
            "1111-2222-3333-4444-5555-6666-7777-8888",
            viewModel.NewRecoveryCode);
    }

    [Fact]
    public async Task
        ConfirmRecoveryCodeSaved_AfterSuccessfulRecovery_FinishesWorkflow()
    {
        var service =
            new TestRecoveryService();

        var viewModel =
            new OwnerPasswordRecoveryViewModel(
                service)
            {
                Username =
                    "owner"
            };

        await viewModel.RecoverCommand
            .ExecuteAsync(
                new OwnerPasswordRecoveryPasswords(
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026",
                    "A new secure owner password 2026"));

        bool finished =
            false;

        viewModel.RecoveryFinished +=
            (_, _) =>
                finished = true;

        viewModel
            .ConfirmRecoveryCodeSavedCommand
            .Execute(
                null);

        Assert.True(
            viewModel.IsCompletionAcknowledged);

        Assert.True(
            finished);
    }

    private sealed class TestRecoveryService
        : IOwnerPasswordRecoveryService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<OwnerPasswordRecoveryResult>
            RecoverAsync(
                OwnerPasswordRecoveryRequest request,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                new OwnerPasswordRecoveryResult(
                    true,
                    NewRecoveryCode:
                        "1111-2222-3333-4444-5555-6666-7777-8888"));
        }
    }
}
