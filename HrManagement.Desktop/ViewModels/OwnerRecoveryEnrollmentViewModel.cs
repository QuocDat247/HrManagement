using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Recovery;

namespace HrManagement.Desktop.ViewModels;

public sealed class OwnerRecoveryEnrollmentViewModel
    : ObservableObject
{
    private readonly IOwnerRecoveryEnrollmentService
        _enrollmentService;

    private string _recoveryCode =
        string.Empty;

    private string? _errorMessage;

    private bool _isBusy;

    private bool _isReady;

    public OwnerRecoveryEnrollmentViewModel(
        IOwnerRecoveryEnrollmentService enrollmentService)
    {
        _enrollmentService =
            enrollmentService;

        ConfirmSavedCommand =
            new AsyncRelayCommand(
                ConfirmSavedAsync,
                CanConfirmSaved);
    }

    public event EventHandler?
        EnrollmentCompleted;

    public string RecoveryCode
    {
        get =>
            _recoveryCode;

        private set =>
            SetProperty(
                ref _recoveryCode,
                value);
    }

    public string? ErrorMessage
    {
        get =>
            _errorMessage;

        private set =>
            SetProperty(
                ref _errorMessage,
                value);
    }

    public bool IsBusy
    {
        get =>
            _isBusy;

        private set
        {
            if (SetProperty(
                    ref _isBusy,
                    value))
            {
                ConfirmSavedCommand
                    .NotifyCanExecuteChanged();

                OnPropertyChanged(
                    nameof(CanSubmit));
            }
        }
    }

    public bool IsReady
    {
        get =>
            _isReady;

        private set
        {
            if (SetProperty(
                    ref _isReady,
                    value))
            {
                ConfirmSavedCommand
                    .NotifyCanExecuteChanged();

                OnPropertyChanged(
                    nameof(CanSubmit));
            }
        }
    }

    public bool CanSubmit =>
        IsReady
        && !IsBusy;

    public IAsyncRelayCommand
        ConfirmSavedCommand
    {
        get;
    }

    public void Prepare()
    {
        if (IsReady)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            RecoveryCode =
                _enrollmentService
                    .GenerateRecoveryCode();

            if (string.IsNullOrWhiteSpace(
                    RecoveryCode))
            {
                ErrorMessage =
                    "Không thể tạo mã khôi phục.";

                return;
            }

            IsReady =
                true;
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tạo mã khôi phục. Vui lòng thử lại.";
        }
    }

    private bool CanConfirmSaved()
    {
        return IsReady
            && !IsBusy;
    }

    private async Task ConfirmSavedAsync()
    {
        ErrorMessage =
            null;

        if (!IsReady
            || string.IsNullOrWhiteSpace(
                RecoveryCode))
        {
            ErrorMessage =
                "Mã khôi phục chưa sẵn sàng.";

            return;
        }

        try
        {
            IsBusy =
                true;

            OwnerRecoveryEnrollmentResult result =
                await _enrollmentService
                    .EnrollAsync(
                        RecoveryCode);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu mã khôi phục.";

                return;
            }

            EnrollmentCompleted?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác thiết lập mã khôi phục đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi lưu mã khôi phục. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
