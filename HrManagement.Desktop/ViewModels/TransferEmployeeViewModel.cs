using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Employees;
using DepartmentEntity =
    HrManagement.Domain.Organization.Departments.Department;
using PositionEntity =
    HrManagement.Domain.Organization.Positions.Position;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class TransferEmployeeViewModel
    : ObservableObject
{
    private readonly IEmployeeOrganizationTransferService
        _transferService;

    private readonly IDepartmentService
        _departmentService;

    private readonly IPositionService
        _positionService;

    private Employee? _employee;

    [ObservableProperty]
    private string employeeDisplayName =
        string.Empty;

    [ObservableProperty]
    private string currentDepartmentName =
        string.Empty;

    [ObservableProperty]
    private string currentPositionName =
        string.Empty;

    [ObservableProperty]
    private DepartmentEntity? selectedDepartment;

    [ObservableProperty]
    private PositionEntity? selectedPosition;

    [ObservableProperty]
    private DateTime effectiveDate =
        DateTime.Today;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<DepartmentEntity>
        DepartmentOptions
    {
        get;
    } = [];

    public ObservableCollection<PositionEntity>
        PositionOptions
    {
        get;
    } = [];

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public event EventHandler? SaveSucceeded;

    public TransferEmployeeViewModel(
        IEmployeeOrganizationTransferService transferService,
        IDepartmentService departmentService,
        IPositionService positionService)
    {
        _transferService =
            transferService;

        _departmentService =
            departmentService;

        _positionService =
            positionService;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync);
    }

    public void LoadEmployee(
        Employee employee)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        _employee =
            employee;

        EmployeeDisplayName =
            $"{employee.EmployeeCode} - {employee.FullName}";

        CurrentDepartmentName =
            employee.Department;

        CurrentPositionName =
            employee.Position;

        EffectiveDate =
            DateTime.Today;

        ErrorMessage =
            null;
    }

    public async Task LoadOrganizationOptionsAsync()
    {
        Employee? employee =
            _employee;

        if (employee is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            IReadOnlyList<DepartmentEntity> departments =
                await _departmentService
                    .GetDepartmentsAsync();

            IReadOnlyList<PositionEntity> positions =
                await _positionService
                    .GetPositionsAsync();

            DepartmentOptions.Clear();

            foreach (DepartmentEntity department
                     in departments
                         .Where(
                             department =>
                                 department.IsActive)
                         .OrderBy(
                             department =>
                                 department.Name)
                         .ThenBy(
                             department =>
                                 department.Code))
            {
                DepartmentOptions.Add(
                    department);
            }

            PositionOptions.Clear();

            foreach (PositionEntity position
                     in positions
                         .Where(
                             position =>
                                 position.IsActive)
                         .OrderBy(
                             position =>
                                 position.Name)
                         .ThenBy(
                             position =>
                                 position.Code))
            {
                PositionOptions.Add(
                    position);
            }

            SelectedDepartment =
                DepartmentOptions
                    .FirstOrDefault(
                        department =>
                            department.Id ==
                            employee.DepartmentId);

            SelectedPosition =
                PositionOptions
                    .FirstOrDefault(
                        position =>
                            position.Id ==
                            employee.PositionId);
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải danh sách phòng ban "
                + "và chức danh.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        Employee? employee =
            _employee;

        if (employee is null)
        {
            ErrorMessage =
                "Không tìm thấy nhân viên.";

            return;
        }

        if (SelectedDepartment is null)
        {
            ErrorMessage =
                "Vui lòng chọn phòng ban.";

            return;
        }

        if (SelectedPosition is null)
        {
            ErrorMessage =
                "Vui lòng chọn chức danh.";

            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request =
                new TransferEmployeeOrganizationRequest(
                    employee.Id,
                    SelectedDepartment.Id,
                    SelectedPosition.Id,
                    DateOnly.FromDateTime(
                        EffectiveDate));

            TransferEmployeeOrganizationResult result =
                await _transferService
                    .TransferAsync(
                        request);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage;

                return;
            }

            SaveSucceeded?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể điều chuyển nhân viên.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
