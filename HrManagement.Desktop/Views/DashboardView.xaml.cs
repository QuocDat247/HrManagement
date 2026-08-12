using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HrManagement.Desktop.ViewModels;
using System.Globalization;
using System.Windows.Threading;

namespace HrManagement.Desktop.Views;

public partial class DashboardView : UserControl
{
    private readonly DispatcherTimer _clockTimer;

    private static readonly CultureInfo VietnameseCulture =
        new("vi-VN");

    public DashboardView()
    {
        InitializeComponent();
        Loaded += DashboardView_Loaded;

        _clockTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
        _clockTimer.Tick += ClockTimer_Tick;
    }

    private async void DashboardView_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        UpdateClock();
        if (!_clockTimer.IsEnabled)
        {
            _clockTimer.Start();
        }

        if (DataContext is DashboardViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private void DashboardContent_PreviewMouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        double newOffset =
            DashboardScrollViewer.VerticalOffset - e.Delta;

        DashboardScrollViewer.ScrollToVerticalOffset(newOffset);

        e.Handled = true;
    }

    private void ClockTimer_Tick(
    object? sender,
    EventArgs e)
    {
        UpdateClock();
    }

    private void UpdateClock()
    {
        DateTime now =
            DateTime.Now;

        CurrentDayText.Text =
            now.ToString(
                "dd",
                VietnameseCulture);

        CurrentMonthYearText.Text =
            $"Tháng {now.Month}, {now.Year}";

        string weekday =
            now.ToString(
                "dddd",
                VietnameseCulture);

        CurrentWeekdayText.Text =
            VietnameseCulture.TextInfo
                .ToTitleCase(weekday);

        CurrentDateText.Text =
            now.ToString(
                "dd/MM/yyyy",
                VietnameseCulture);

        CurrentTimeText.Text =
            now.ToString(
                "HH:mm:ss",
                VietnameseCulture);
    }

    private void DashboardView_Unloaded(
    object sender,
    RoutedEventArgs e)
    {
        _clockTimer.Stop();
    }
}
