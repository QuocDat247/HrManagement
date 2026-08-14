using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Employees;
using DepartmentEntity =
    HrManagement.Domain.Organization.Departments.Department;

using PositionEntity =
    HrManagement.Domain.Organization.Positions.Position;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class AddEmployeeViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;

    [ObservableProperty]
    private string employeeCode = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? phoneNumber;

    [ObservableProperty]
    private DateTime? dateOfBirth;

    [ObservableProperty]
    private DateTime hireDate = DateTime.Today;

    [ObservableProperty]
    private EmployeeStatus selectedStatus = EmployeeStatus.Active;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private DepartmentEntity? selectedDepartment;

    [ObservableProperty]
    private PositionEntity? selectedPosition;


    public IReadOnlyList<EmployeeStatus> StatusOptions { get; } =
    [
        EmployeeStatus.Active,
        EmployeeStatus.OnLeave
    ];

    private readonly IDepartmentService _departmentService;
    private readonly IPositionService _positionService;
    public IAsyncRelayCommand SaveCommand { get; }

    public event EventHandler? SaveSucceeded;

    public AddEmployeeViewModel(
        IEmployeeService employeeService,
        IDepartmentService departmentService,
        IPositionService positionService)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
        _positionService = positionService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public ObservableCollection<DepartmentEntity>
    DepartmentOptions
    { get; } = [];

    public ObservableCollection<PositionEntity>
        PositionOptions
    { get; } = [];

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (SelectedDepartment is null)
            {
                ErrorMessage = "Vui lòng chọn phòng ban.";
                return;
            }

            if (SelectedPosition is null)
            {
                ErrorMessage = "Vui lòng chọn chức danh.";
                return;
            }

            var request = new CreateEmployeeRequest(
                EmployeeCode,
                FullName,
                Email,
                PhoneNumber,
                DateOfBirth.HasValue
                    ? DateOnly.FromDateTime(DateOfBirth.Value)
                    : null,
                DateOnly.FromDateTime(HireDate),
                SelectedDepartment.Id,
                SelectedPosition.Id,
                SelectedStatus);

            CreateEmployeeResult result =
                await _employeeService.CreateEmployeeAsync(request);

            if (!result.IsSuccessful)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể thêm nhân viên.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadOrganizationOptionsAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            IReadOnlyList<DepartmentEntity> departments =
                await _departmentService.GetDepartmentsAsync();

            IReadOnlyList<PositionEntity> positions =
                await _positionService.GetPositionsAsync();

            DepartmentOptions.Clear();

            foreach (DepartmentEntity department in departments
                         .Where(department => department.IsActive)
                         .OrderBy(department => department.Name)
                         .ThenBy(department => department.Code))
            {
                DepartmentOptions.Add(department);
            }

            PositionOptions.Clear();

            foreach (PositionEntity position in positions
                         .Where(position => position.IsActive)
                         .OrderBy(position => position.Name)
                         .ThenBy(position => position.Code))
            {
                PositionOptions.Add(position);
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải danh sách phòng ban và chức danh.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
