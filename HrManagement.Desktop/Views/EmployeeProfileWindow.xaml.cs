using System.Windows;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HrManagement.Desktop.Views;

public partial class EmployeeProfileWindow
    : Window
{
    private readonly EmployeeProfileViewModel
        _viewModel;

    private Employee? _employee;

    private bool _loaded;

    public EmployeeProfileWindow(
        EmployeeProfileViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            viewModel;
    }

    public void LoadEmployee(
        Employee employee)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        _employee =
            employee;

        _loaded =
            false;
    }

    private void EmergencyContactsList_PreviewMouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        HandleNestedListMouseWheel(
            sender,
            e);
    }

    private void IdentificationRecordsList_PreviewMouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        HandleNestedListMouseWheel(
            sender,
            e);
    }


    private static void HandleNestedListMouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        ScrollViewer? innerScrollViewer =
            FindVisualChild<ScrollViewer>(
                listBox);

        if (innerScrollViewer is null)
        {
            return;
        }

        bool canScrollUp =
            e.Delta > 0
            && innerScrollViewer.VerticalOffset > 0;

        bool canScrollDown =
            e.Delta < 0
            && innerScrollViewer.VerticalOffset
                < innerScrollViewer.ScrollableHeight;

        if (canScrollUp || canScrollDown)
        {
            return;
        }

        e.Handled =
            true;

        var forwardedEvent =
            new MouseWheelEventArgs(
                e.MouseDevice,
                e.Timestamp,
                e.Delta)
            {
                RoutedEvent =
                    Mouse.MouseWheelEvent,

                Source =
                    listBox
            };

        listBox.RaiseEvent(
            forwardedEvent);
    }

    private static T? FindVisualChild<T>(
        DependencyObject parent)
        where T : DependencyObject
    {
        int childCount =
            VisualTreeHelper.GetChildrenCount(
                parent);

        for (int index = 0;
             index < childCount;
             index++)
        {
            DependencyObject child =
                VisualTreeHelper.GetChild(
                    parent,
                    index);

            if (child is T match)
            {
                return match;
            }

            T? descendant =
                FindVisualChild<T>(
                    child);

            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
    private async void EmployeeProfileWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_loaded
            || _employee is null)
        {
            return;
        }

        _loaded =
            true;

        await _viewModel
            .LoadEmployeeAsync(
                _employee);
    }

    private void ProfileSectionNavigation_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not string section)
        {
            return;
        }

        FrameworkElement? target =
            section switch
            {
                "Personal" =>
                    PersonalSection,

                "Addresses" =>
                    AddressesSection,

                "Emergency" =>
                    EmergencyContactsSection,

                "Identification" =>
                    IdentificationRecordsSection,

                _ =>
                    null
            };

        if (target is null)
        {
            return;
        }

        ScrollToProfileSection(
            target);
    }

    // Navigation handler
    private void ScrollToProfileSection(
        FrameworkElement target)
    {
        if (!target.IsLoaded)
        {
            return;
        }

        GeneralTransform transform =
            target.TransformToAncestor(
                ProfileScrollViewer);

        Point position =
            transform.Transform(
                new Point(
                    0,
                    0));

        double targetOffset =
            ProfileScrollViewer.VerticalOffset
            + position.Y;

        ProfileScrollViewer.ScrollToVerticalOffset(
            Math.Max(
                0,
                targetOffset));
    }
}
