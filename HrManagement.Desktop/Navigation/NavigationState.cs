using System.Windows;

namespace HrManagement.Desktop.Navigation;

public static class NavigationState
{
    public static readonly DependencyProperty
        IsActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsActive",
                typeof(bool),
                typeof(NavigationState),
                new FrameworkPropertyMetadata(false));

    public static bool GetIsActive(
        DependencyObject element)
    {
        return (bool)element.GetValue(
            IsActiveProperty);
    }

    public static void SetIsActive(
        DependencyObject element,
        bool value)
    {
        element.SetValue(
            IsActiveProperty,
            value);
    }
}
