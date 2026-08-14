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
}
