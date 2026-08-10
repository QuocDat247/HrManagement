using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Services;

public interface IEmployeeDialogService
{
    bool ShowAddEmployeeDialog();

    bool ShowEditEmployeeDialog(Employee employee);

    bool ConfirmDeactivateEmployee(Employee employee);
}
