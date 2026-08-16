using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeOrganizationHistoryViewModel
    : ObservableObject
{
    private readonly IEmployeeOrganizationHistoryService
        _historyService;

    [ObservableProperty]
    private string employeeCode =
        string.Empty;

    [ObservableProperty]
    private string fullName =
        string.Empty;

    [ObservableProperty]
    private IReadOnlyList<OrganizationAssignmentHistoryViewItem>
        assignments =
            Array.Empty<OrganizationAssignmentHistoryViewItem>();

    [ObservableProperty]
    private bool hasAssignments;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public EmployeeOrganizationHistoryViewModel(
        IEmployeeOrganizationHistoryService historyService)
    {
        _historyService =
            historyService;
    }

    public async Task LoadAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        EmployeeCode =
            employee.EmployeeCode;

        FullName =
            employee.FullName;

        IsLoading =
            true;

        HasError =
            false;

        ErrorMessage =
            null;

        Assignments =
            Array.Empty<OrganizationAssignmentHistoryViewItem>();

        HasAssignments =
            false;

        try
        {
            EmployeeOrganizationAssignmentHistoryDetails details =
                await _historyService
                    .GetHistoryAsync(
                        employee.Id,
                        cancellationToken);

            List<OrganizationAssignmentHistoryViewItem> items =
                details.Assignments
                    .Select(
                        assignment =>
                            new OrganizationAssignmentHistoryViewItem(
                                SequenceNumber:
                                    assignment.SequenceNumber,

                                Title:
                                    assignment.IsBaseline
                                        ? $"Phân công {assignment.SequenceNumber} — Dữ liệu nền"
                                        : $"Phân công {assignment.SequenceNumber}",

                                DateRange:
                                    BuildDateRange(
                                        assignment),

                                DepartmentText:
                                    $"{assignment.DepartmentCode} — "
                                    + assignment.DepartmentName,

                                PositionText:
                                    $"{assignment.PositionCode} — "
                                    + assignment.PositionName,

                                StatusText:
                                    assignment.IsOpen
                                        ? "Hiện tại"
                                        : "Đã kết thúc",

                                BaselineNote:
                                    assignment.IsBaseline
                                        ? "Dữ liệu nền được suy ra khi nâng cấp hệ thống; "
                                          + "không khẳng định đây là ngày điều chuyển thực tế."
                                        : null,

                                IsOpen:
                                    assignment.IsOpen,

                                IsBaseline:
                                    assignment.IsBaseline))
                    .ToList();

            Assignments =
                items;

            HasAssignments =
                items.Count > 0;
        }
        catch (Exception)
        {
            Assignments =
                Array.Empty<OrganizationAssignmentHistoryViewItem>();

            HasAssignments =
                false;

            HasError =
                true;

            ErrorMessage =
                "Không thể tải lịch sử phân công tổ chức.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private static string BuildDateRange(
        OrganizationAssignmentHistoryItem assignment)
    {
        string startDate =
            assignment.StartDate.ToString(
                "dd/MM/yyyy");

        string endDate =
            assignment.EndDate.HasValue
                ? assignment.EndDate.Value.ToString(
                    "dd/MM/yyyy")
                : "Hiện tại";

        return $"{startDate} → {endDate}";
    }
}
