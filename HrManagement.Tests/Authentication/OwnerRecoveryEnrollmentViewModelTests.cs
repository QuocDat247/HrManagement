using HrManagement.Application.Authentication.Recovery;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Authentication;

public sealed class OwnerRecoveryEnrollmentViewModelTests
{
    [Fact]
    public void
        Prepare_GeneratesRecoveryCodeWithoutPersisting()
    {
        var service =
            new TestEnrollmentService();

        var viewModel =
            new OwnerRecoveryEnrollmentViewModel(
                service);

        viewModel.Prepare();

        Assert.True(
            viewModel.IsReady);

        Assert.Equal(
            "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
            viewModel.RecoveryCode);

        Assert.False(
            service.EnrollCalled);
    }

    [Fact]
    public async Task
        ConfirmSavedCommand_WhenSuccessful_RaisesEnrollmentCompleted()
    {
        var service =
            new TestEnrollmentService();

        var viewModel =
            new OwnerRecoveryEnrollmentViewModel(
                service);

        viewModel.Prepare();

        bool completed =
            false;

        viewModel.EnrollmentCompleted +=
            (_, _) =>
                completed = true;

        await viewModel
            .ConfirmSavedCommand
            .ExecuteAsync(
                null);

        Assert.True(
            service.EnrollCalled);

        Assert.Equal(
            viewModel.RecoveryCode,
            service.RecoveryCode);

        Assert.True(
            completed);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task
        ConfirmSavedCommand_WhenPersistenceFails_DoesNotComplete()
    {
        var service =
            new TestEnrollmentService
            {
                Result =
                    new OwnerRecoveryEnrollmentResult(
                        false,
                        "Không thể lưu mã khôi phục.")
            };

        var viewModel =
            new OwnerRecoveryEnrollmentViewModel(
                service);

        viewModel.Prepare();

        bool completed =
            false;

        viewModel.EnrollmentCompleted +=
            (_, _) =>
                completed = true;

        await viewModel
            .ConfirmSavedCommand
            .ExecuteAsync(
                null);

        Assert.False(
            completed);

        Assert.Equal(
            "Không thể lưu mã khôi phục.",
            viewModel.ErrorMessage);
    }

    private sealed class TestEnrollmentService
        : IOwnerRecoveryEnrollmentService
    {
        public OwnerRecoveryEnrollmentResult Result
        {
            get;
            set;
        } =
            new(
                true);

        public bool EnrollCalled
        {
            get;
            private set;
        }

        public string? RecoveryCode
        {
            get;
            private set;
        }

        public Task<bool> IsEnrollmentRequiredAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                true);
        }

        public string GenerateRecoveryCode()
        {
            return
                "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA";
        }

        public Task<OwnerRecoveryEnrollmentResult>
            EnrollAsync(
                string recoveryCode,
                CancellationToken cancellationToken = default)
        {
            EnrollCalled =
                true;

            RecoveryCode =
                recoveryCode;

            return Task.FromResult(
                Result);
        }
    }
}
