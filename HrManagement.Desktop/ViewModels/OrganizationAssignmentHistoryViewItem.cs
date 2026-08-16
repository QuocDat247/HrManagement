namespace HrManagement.Desktop.ViewModels;

public sealed record OrganizationAssignmentHistoryViewItem(
    int SequenceNumber,
    string Title,
    string DateRange,
    string DepartmentText,
    string PositionText,
    string StatusText,
    string? BaselineNote,
    bool IsOpen,
    bool IsBaseline);
