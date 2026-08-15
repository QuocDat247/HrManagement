using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Application.Organization.Memberships;
using HrManagement.Domain.Employees;
using DepartmentEntity =
    HrManagement.Domain.Organization.Departments.Department;
using PositionEntity =
    HrManagement.Domain.Organization.Positions.Position;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class OrganizationEmployeesViewModel
    : ObservableObject
{
    private readonly IOrganizationMembershipQueryService
        _membershipQueryService;

    private Guid? _departmentId;
    private Guid? _positionId;

    [ObservableProperty]
    private string windowTitle =
        "Danh sách nhân viên";

    [ObservableProperty]
    private string heading =
        "Danh sách nhân viên";

    [ObservableProperty]
    private string contextName =
        string.Empty;

    [ObservableProperty]
    private string contextCode =
        string.Empty;

    [ObservableProperty]
    private string emptyMessage =
        "Chưa có nhân viên.";

    [ObservableProperty]
    private IReadOnlyList<OrganizationEmployeeViewItem>
        employees =
            Array.Empty<OrganizationEmployeeViewItem>();

    [ObservableProperty]
    private bool hasEmployees;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public OrganizationEmployeesViewModel(
        IOrganizationMembershipQueryService membershipQueryService)
    {
        _membershipQueryService =
            membershipQueryService;
    }

    public void ConfigureForDepartment(
        DepartmentEntity department)
    {
        ArgumentNullException.ThrowIfNull(
            department);

        _departmentId =
            department.Id;

        _positionId =
            null;

        WindowTitle =
            $"Nhân viên phòng ban - {department.Name}";

        Heading =
            "Nhân viên thuộc phòng ban";

        ContextName =
            department.Name;

        ContextCode =
            department.Code;

        EmptyMessage =
            $"Phòng ban \"{department.Name}\" chưa có nhân viên.";
    }

    public void ConfigureForPosition(
        PositionEntity position)
    {
        ArgumentNullException.ThrowIfNull(
            position);

        _positionId =
            position.Id;

        _departmentId =
            null;

        WindowTitle =
            $"Nhân viên chức danh - {position.Name}";

        Heading =
            "Nhân viên theo chức danh";

        ContextName =
            position.Name;

        ContextCode =
            position.Code;

        EmptyMessage =
            $"Chưa có nhân viên mang chức danh \"{position.Name}\".";
    }

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        Employees =
            Array.Empty<OrganizationEmployeeViewItem>();

        HasEmployees = false;

        try
        {
            IReadOnlyList<OrganizationEmployeeListItem> result;

            if (_departmentId.HasValue)
            {
                result =
                    await _membershipQueryService
                        .GetEmployeesByDepartmentAsync(
                            _departmentId.Value,
                            cancellationToken);
            }
            else if (_positionId.HasValue)
            {
                result =
                    await _membershipQueryService
                        .GetEmployeesByPositionAsync(
                            _positionId.Value,
                            cancellationToken);
            }
            else
            {
                throw new InvalidOperationException(
                    "Chưa chọn phòng ban hoặc chức danh.");
            }

            List<OrganizationEmployeeViewItem> items =
                result
                    .Select(Map)
                    .ToList();

            Employees =
                items;

            HasEmployees =
                items.Count > 0;
        }
        catch (Exception)
        {
            Employees =
                Array.Empty<OrganizationEmployeeViewItem>();

            HasEmployees = false;
            HasError = true;

            ErrorMessage =
                "Không thể tải danh sách nhân viên.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static OrganizationEmployeeViewItem Map(
        OrganizationEmployeeListItem employee)
    {
        return new OrganizationEmployeeViewItem(
            employee.EmployeeId,
            employee.EmployeeCode,
            employee.FullName,
            employee.DepartmentName,
            employee.PositionName,
            GetStatusText(employee.Status),
            employee.HireDate.ToString("dd/MM/yyyy"));
    }

    private static string GetStatusText(
        EmployeeStatus status)
    {
        return status switch
        {
            EmployeeStatus.Active =>
                "Đang làm việc",

            EmployeeStatus.OnLeave =>
                "Nghỉ phép",

            EmployeeStatus.Inactive =>
                "Ngừng hoạt động",

            _ =>
                status.ToString()
        };
    }
}
