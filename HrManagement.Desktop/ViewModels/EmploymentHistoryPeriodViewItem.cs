namespace HrManagement.Desktop.ViewModels;

public sealed record EmploymentHistoryPeriodViewItem(
    int SequenceNumber,
    string Title,
    string DateRange,
    string StatusText,
    bool IsOpen);
