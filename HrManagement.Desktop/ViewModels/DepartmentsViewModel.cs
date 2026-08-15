using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Organization.Departments;
using HrManagement.Desktop.Services.Departments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Application.Organization.Memberships;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class DepartmentsViewModel
    : ObservableObject
{
    private readonly IDepartmentService
        _departmentService;

    private readonly IDepartmentDialogService
        _departmentDialogService;

    private readonly IOrganizationMembershipQueryService
        _membershipQueryService;

    [ObservableProperty]
    private IReadOnlyList<Department>
        departments =
            Array.Empty<Department>();

    [ObservableProperty]
    private Department? selectedDepartment;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private IReadOnlyList<DepartmentStaffingViewItem>
    departmentRows =
        Array.Empty<DepartmentStaffingViewItem>();

    [ObservableProperty]
    private DepartmentStaffingViewItem?
        selectedDepartmentRow;

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand AddDepartmentCommand
    {
        get;
    }

    public IAsyncRelayCommand EditDepartmentCommand
    {
        get;
    }

    public IAsyncRelayCommand DeactivateDepartmentCommand
    {
        get;
    }

    public IAsyncRelayCommand ReactivateDepartmentCommand
    {
        get;
    }

    public IRelayCommand ViewEmployeesCommand
    {
        get;
    }

    public DepartmentsViewModel(
        IDepartmentService departmentService,
        IDepartmentDialogService departmentDialogService,
        IOrganizationMembershipQueryService membershipQueryService)
    {
        _departmentService =
            departmentService;

        _departmentDialogService =
            departmentDialogService;

        _membershipQueryService =
            membershipQueryService;

        ViewEmployeesCommand =
            new RelayCommand(
                ViewEmployees,
                CanViewEmployees);

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        AddDepartmentCommand =
            new AsyncRelayCommand(
                AddDepartmentAsync);

        EditDepartmentCommand =
            new AsyncRelayCommand(
                EditDepartmentAsync,
                CanEditDepartment);

        DeactivateDepartmentCommand =
            new AsyncRelayCommand(
                DeactivateDepartmentAsync,
                CanDeactivateDepartment);

        ReactivateDepartmentCommand =
            new AsyncRelayCommand(
                ReactivateDepartmentAsync,
                CanReactivateDepartment);
    }

    public async Task LoadAsync()
    {
        IReadOnlyList<Department> loadedDepartments =
            await _departmentService
                .GetDepartmentsAsync();

        IReadOnlyList<OrganizationStaffingCount> staffingCounts =
            await _membershipQueryService
                .GetDepartmentStaffingCountsAsync();

        Dictionary<Guid, OrganizationStaffingCount> countsById =
            staffingCounts.ToDictionary(
                count => count.OrganizationId);

        Departments = loadedDepartments;
        IsLoading = true;
        ErrorMessage = null;

        DepartmentRows = loadedDepartments
        .Select(department =>
        {
            OrganizationStaffingCount staffing =
                countsById.TryGetValue(
                    department.Id,
                    out OrganizationStaffingCount? count)
                    ? count
                    : new OrganizationStaffingCount(
                        department.Id,
                        ActiveCount: 0,
                        OnLeaveCount: 0,
                        InactiveCount: 0);

            return new DepartmentStaffingViewItem(
                department,
                staffing);
        })
        .ToList();

        try
        {
            Departments =
                await _departmentService
                    .GetDepartmentsAsync();
        }
        catch (Exception)
        {
            Departments =
                Array.Empty<Department>();

            DepartmentRows =
                Array.Empty<DepartmentStaffingViewItem>();

            ErrorMessage =
                "Không thể tải danh sách phòng ban.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedDepartmentRowChanged(
    DepartmentStaffingViewItem? value)
    {
        SelectedDepartment =
            value?.Department;
    }

    private bool CanViewEmployees()
    {
        return SelectedDepartment is not null;
    }

    private void ViewEmployees()
    {
        Department? department =
            SelectedDepartment;

        if (department is null)
        {
            return;
        }

        _departmentDialogService
            .ShowEmployees(
                department);
    }

    private async Task AddDepartmentAsync()
    {
        DepartmentEditorDialogResult? dialogResult =
            _departmentDialogService
                .ShowAddDepartmentDialog();

        if (dialogResult is null)
        {
            return;
        }

        ErrorMessage = null;

        DepartmentOperationResult result =
            await _departmentService
                .CreateDepartmentAsync(
                    new CreateDepartmentRequest(
                        dialogResult.Code,
                        dialogResult.Name));

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedDepartment = null;

        await LoadAsync();
    }

    private bool CanEditDepartment()
    {
        return SelectedDepartment is not null;
    }

    private async Task EditDepartmentAsync()
    {
        Department? department =
            SelectedDepartment;

        if (department is null)
        {
            return;
        }

        DepartmentEditorDialogResult? dialogResult =
            _departmentDialogService
                .ShowEditDepartmentDialog(
                    department);

        if (dialogResult is null)
        {
            return;
        }

        ErrorMessage = null;

        DepartmentOperationResult result =
            await _departmentService
                .UpdateDepartmentAsync(
                    new UpdateDepartmentRequest(
                        department.Id,
                        dialogResult.Code,
                        dialogResult.Name));

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedDepartment = null;

        await LoadAsync();
    }

    private bool CanDeactivateDepartment()
    {
        return SelectedDepartment?.IsActive == true;
    }

    private async Task DeactivateDepartmentAsync()
    {
        Department? department =
            SelectedDepartment;

        if (department is null
            || !department.IsActive)
        {
            return;
        }

        bool confirmed =
            _departmentDialogService
                .ConfirmDeactivateDepartment(
                    department);

        if (!confirmed)
        {
            return;
        }

        ErrorMessage = null;

        DepartmentOperationResult result =
            await _departmentService
                .DeactivateDepartmentAsync(
                    department.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedDepartment = null;

        await LoadAsync();
    }

    private bool CanReactivateDepartment()
    {
        return SelectedDepartment?.IsActive == false;
    }

    private async Task ReactivateDepartmentAsync()
    {
        Department? department =
            SelectedDepartment;

        if (department is null
            || department.IsActive)
        {
            return;
        }

        bool confirmed =
            _departmentDialogService
                .ConfirmReactivateDepartment(
                    department);

        if (!confirmed)
        {
            return;
        }

        ErrorMessage = null;

        DepartmentOperationResult result =
            await _departmentService
                .ReactivateDepartmentAsync(
                    department.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedDepartment = null;

        await LoadAsync();
    }

    partial void OnSelectedDepartmentChanged(
        Department? value)
    {
        EditDepartmentCommand
            .NotifyCanExecuteChanged();

        DeactivateDepartmentCommand
            .NotifyCanExecuteChanged();

        ReactivateDepartmentCommand
            .NotifyCanExecuteChanged();

        ViewEmployeesCommand
            .NotifyCanExecuteChanged();

        if (value is null
            && SelectedDepartmentRow is not null)
        {
            SelectedDepartmentRow =
                null;
        }
    }
}
