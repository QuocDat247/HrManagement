using HrManagement.Desktop.Views;
using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Desktop.Services.Departments;

public interface IDepartmentDialogService
{
    DepartmentEditorDialogResult?
        ShowAddDepartmentDialog();

    DepartmentEditorDialogResult?
        ShowEditDepartmentDialog(
            Department department);

    bool ConfirmDeactivateDepartment(
        Department department);

    bool ConfirmReactivateDepartment(
        Department department);

    // CHỈ KHAI BÁO DÒNG NÀY:
    void ShowEmployees(
        Department department);
}
