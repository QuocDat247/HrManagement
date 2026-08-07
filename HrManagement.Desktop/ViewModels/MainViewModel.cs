using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HrManagement.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string _currentPageTitle = "Tổng quan";

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand<string>(Navigate);
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        private set => SetProperty(ref _currentPageTitle, value);
    }

    public IRelayCommand<string> NavigateCommand { get; }

    private void Navigate(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
        {
            return;
        }

        CurrentPageTitle = page;
    }
}