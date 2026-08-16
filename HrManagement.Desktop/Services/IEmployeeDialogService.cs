using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Services;

public interface IEmployeeDialogService
{
    bool ShowAddEmployeeDialog();

    bool ShowEditEmployeeDialog(
        Employee employee);

    DateOnly? ShowDeactivateEmployeeDialog(
        Employee employee);

    EmployeeStatus? ShowCancelEmployeeDeactivationDialog(
        Employee employee);

    RehireEmployeeDialogResult?
        ShowRehireEmployeeDialog(
            Employee employee);

    void ShowEmploymentHistoryDialog(
        Employee employee);

    bool ShowTransferEmployeeDialog(
        Employee employee);

    void ShowOrganizationHistoryDialog(
        Employee employee);
}
