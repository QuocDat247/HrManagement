using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeEmploymentHistoryViewModel
    : ObservableObject
{
    private readonly IEmploymentHistoryService
        _employmentHistoryService;

    [ObservableProperty]
    private string employeeCode = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<EmploymentHistoryPeriodViewItem>
        periods =
            Array.Empty<EmploymentHistoryPeriodViewItem>();

    [ObservableProperty]
    private bool hasPeriods;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public EmployeeEmploymentHistoryViewModel(
        IEmploymentHistoryService employmentHistoryService)
    {
        _employmentHistoryService =
            employmentHistoryService;
    }

    public async Task LoadAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        EmployeeCode =
            employee.EmployeeCode;

        FullName =
            employee.FullName;

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        Periods =
            Array.Empty<EmploymentHistoryPeriodViewItem>();

        HasPeriods = false;

        try
        {
            EmployeeEmploymentHistoryDetails details =
                await _employmentHistoryService
                    .GetHistoryAsync(
                        employee.Id,
                        cancellationToken);

            List<EmploymentHistoryPeriodViewItem> items =
                details.Periods
                    .Select(period =>
                        new EmploymentHistoryPeriodViewItem(
                            SequenceNumber:
                                period.SequenceNumber,

                            Title:
                                $"Giai đoạn {period.SequenceNumber}",

                            DateRange:
                                BuildDateRange(period),

                            StatusText:
                                period.IsOpen
                                    ? "Đang làm việc"
                                    : "Đã kết thúc",

                            IsOpen:
                                period.IsOpen))
                    .ToList();

            Periods = items;
            HasPeriods = items.Count > 0;
        }
        catch (Exception)
        {
            Periods =
                Array.Empty<EmploymentHistoryPeriodViewItem>();

            HasPeriods = false;
            HasError = true;

            ErrorMessage =
                "Không thể tải lịch sử làm việc.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildDateRange(
        EmploymentHistoryPeriodItem period)
    {
        string startDate =
            period.StartDate.ToString(
                "dd/MM/yyyy");

        string endDate =
            period.EndDate.HasValue
                ? period.EndDate.Value.ToString(
                    "dd/MM/yyyy")
                : "Hiện tại";

        return $"{startDate} → {endDate}";
    }
}
