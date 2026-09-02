using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Theming;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class SettingsViewModel
    : ObservableObject
{
    private readonly IApplicationThemeService
        _themeService;

    [ObservableProperty]
    private ApplicationAppearance selectedAppearance;

    [ObservableProperty]
    private ApplicationAccent selectedAccent;

    [ObservableProperty]
    private bool isApplying;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

    public SettingsViewModel(
        IApplicationThemeService themeService)
    {
        _themeService =
            themeService;

        AppearanceOptions =
        [
            new SettingsAppearanceOption(
                ApplicationAppearance.System,
                "Theo hệ thống",
                "Tự động dùng giao diện sáng hoặc tối theo Windows."),

            new SettingsAppearanceOption(
                ApplicationAppearance.Light,
                "Sáng",
                "Sử dụng giao diện sáng cho toàn bộ ứng dụng."),

            new SettingsAppearanceOption(
                ApplicationAppearance.Dark,
                "Tối",
                "Sử dụng giao diện tối cho toàn bộ ứng dụng.")
        ];

        AccentOptions =
        [
            new SettingsAccentOption(
                ApplicationAccent.Blue,
                "Xanh dương",
                "Màu chủ đạo cân bằng, phù hợp giao diện quản trị."),

            new SettingsAccentOption(
                ApplicationAccent.Green,
                "Xanh lá",
                "Màu chủ đạo xanh lá cho navigation và hành động chính.")
        ];

        LoadCommand =
            new RelayCommand(
                Load);

        ApplyCommand =
            new AsyncRelayCommand(
                ApplyAsync,
                CanApply);

        Load();
    }

    public IReadOnlyList<SettingsAppearanceOption>
        AppearanceOptions
    {
        get;
    }

    public IReadOnlyList<SettingsAccentOption>
        AccentOptions
    {
        get;
    }

    public IRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand ApplyCommand
    {
        get;
    }

    public bool HasChanges =>
        SelectedAppearance !=
            _themeService.CurrentPreference.Appearance
        || SelectedAccent !=
            _themeService.CurrentPreference.Accent;

    public bool CanApplyChanges =>
        CanApply();

    public string CurrentThemeText
    {
        get
        {
            string appearance =
                _themeService.CurrentPreference.Appearance switch
                {
                    ApplicationAppearance.System =>
                        "Theo hệ thống",

                    ApplicationAppearance.Light =>
                        "Sáng",

                    ApplicationAppearance.Dark =>
                        "Tối",

                    _ =>
                        "Không xác định"
                };

            string accent =
                _themeService.CurrentPreference.Accent switch
                {
                    ApplicationAccent.Blue =>
                        "Xanh dương",

                    ApplicationAccent.Green =>
                        "Xanh lá",

                    _ =>
                        "Không xác định"
                };

            return
                $"{appearance} • {accent}";
        }
    }

    public string EffectiveAppearanceText =>
        _themeService.EffectiveAppearance switch
        {
            ApplicationAppearance.Dark =>
                "Hiện đang hiển thị giao diện tối.",

            _ =>
                "Hiện đang hiển thị giao diện sáng."
        };

    partial void OnSelectedAppearanceChanged(
        ApplicationAppearance value)
    {
        NotifySelectionState();
    }

    partial void OnSelectedAccentChanged(
        ApplicationAccent value)
    {
        NotifySelectionState();
    }

    private void Load()
    {
        ApplicationThemePreference preference =
            _themeService.CurrentPreference;

        SelectedAppearance =
            preference.Appearance;

        SelectedAccent =
            preference.Accent;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifySelectionState();
    }

    private async Task ApplyAsync()
    {
        if (!CanApply())
        {
            return;
        }

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        IsApplying =
            true;

        NotifySelectionState();

        try
        {
            var preference =
                new ApplicationThemePreference(
                    SelectedAppearance,
                    SelectedAccent);

            await _themeService.ApplyAsync(
                preference);

            SuccessMessage =
                "Đã áp dụng và lưu cài đặt giao diện.";

            OnPropertyChanged(
                nameof(CurrentThemeText));

            OnPropertyChanged(
                nameof(EffectiveAppearanceText));
        }
        catch (ArgumentException exception)
        {
            ErrorMessage =
                CleanArgumentMessage(
                    exception);
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage =
                exception.Message;
        }
        catch (IOException exception)
        {
            ErrorMessage =
                $"Không thể lưu cài đặt giao diện: {exception.Message}";
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage =
                "Ứng dụng không có quyền lưu cài đặt giao diện.";
        }
        finally
        {
            IsApplying =
                false;

            NotifySelectionState();
        }
    }

    private bool CanApply()
    {
        return !IsApplying
            && HasChanges;
    }

    private void NotifySelectionState()
    {
        OnPropertyChanged(
            nameof(HasChanges));

        OnPropertyChanged(
            nameof(CanApplyChanges));

        ApplyCommand?
            .NotifyCanExecuteChanged();
    }

    private static string CleanArgumentMessage(
        ArgumentException exception)
    {
        if (!string.IsNullOrWhiteSpace(
                exception.ParamName))
        {
            int markerIndex =
                exception.Message.IndexOf(
                    " (Parameter '",
                    StringComparison.Ordinal);

            if (markerIndex >= 0)
            {
                return exception.Message[
                    ..markerIndex];
            }
        }

        return exception.Message;
    }
}
