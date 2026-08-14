using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using System.Collections.ObjectModel;

using DepartmentEntity =
    HrManagement.Domain.Organization.Departments.Department;

using PositionEntity =
    HrManagement.Domain.Organization.Positions.Position;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EditEmployeeViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly IPositionService _positionService;

    private Guid _employeeId;
    private Guid? _currentDepartmentId;
    private Guid? _currentPositionId;

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
    private IReadOnlyList<EmployeeStatus> statusOptions =
    [
        EmployeeStatus.Active,
        EmployeeStatus.OnLeave
    ];

    [ObservableProperty]
    private DepartmentEntity? selectedDepartment;

    [ObservableProperty]
    private PositionEntity? selectedPosition;

    public IAsyncRelayCommand SaveCommand { get; }
    public ObservableCollection<DepartmentEntity>
    DepartmentOptions
    { get; } = [];

    public ObservableCollection<PositionEntity>
        PositionOptions
    { get; } = [];

    public event EventHandler? SaveSucceeded;

    public EditEmployeeViewModel(
    IEmployeeService employeeService,
    IDepartmentService departmentService,
    IPositionService positionService)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
        _positionService = positionService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public void LoadEmployee(Employee employee)
    {
        _currentDepartmentId =
            employee.DepartmentId;

        _currentPositionId =
            employee.PositionId;

        _employeeId = employee.Id;

        EmployeeCode = employee.EmployeeCode;
        FullName = employee.FullName;
        Email = employee.Email;
        PhoneNumber = employee.PhoneNumber;

        DateOfBirth = employee.DateOfBirth.HasValue
            ? employee.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
            : null;

        HireDate =
            employee.HireDate.ToDateTime(TimeOnly.MinValue);

        StatusOptions =
        employee.Status == EmployeeStatus.Inactive
        ? [EmployeeStatus.Inactive]
        :
        [
            EmployeeStatus.Active,
            EmployeeStatus.OnLeave
        ];

        SelectedStatus = employee.Status;
        SelectedStatus = employee.Status;

        ErrorMessage = null;
    }

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

            var request = new UpdateEmployeeRequest(
            _employeeId,
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

            UpdateEmployeeResult result =
                await _employeeService.UpdateEmployeeAsync(request);

            if (!result.IsSuccessful)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể cập nhật nhân viên.";
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
                         .Where(department =>
                             department.IsActive
                             || department.Id == _currentDepartmentId)
                         .OrderBy(department => department.Name)
                         .ThenBy(department => department.Code))
            {
                DepartmentOptions.Add(department);
            }

            PositionOptions.Clear();

            foreach (PositionEntity position in positions
                         .Where(position =>
                             position.IsActive
                             || position.Id == _currentPositionId)
                         .OrderBy(position => position.Name)
                         .ThenBy(position => position.Code))
            {
                PositionOptions.Add(position);
            }

            SelectedDepartment =
                DepartmentOptions.FirstOrDefault(
                    department =>
                        department.Id == _currentDepartmentId);

            SelectedPosition =
                PositionOptions.FirstOrDefault(
                    position =>
                        position.Id == _currentPositionId);
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
