namespace HrManagement.Desktop.Navigation;

public sealed class NavigationItem
{
    public NavigationItem(
        string title,
        Type viewModelType)
    {
        Title = title;
        ViewModelType = viewModelType;
    }

    public string Title { get; }

    public Type ViewModelType { get; }
}