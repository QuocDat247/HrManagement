namespace HrManagement.Domain.Authorization.Permissions;

public static class PermissionCodes
{
    public const string EmployeeView =
        "Employee.View";

    public const string EmployeeCreate =
        "Employee.Create";

    public const string EmployeeEdit =
        "Employee.Edit";

    public const string EmployeeManageLifecycle =
        "Employee.ManageLifecycle";

    public const string AttendanceView =
        "Attendance.View";

    public const string AttendanceEdit =
        "Attendance.Edit";

    public const string AttendanceApprove =
        "Attendance.Approve";

    public const string LeaveView =
        "Leave.View";

    public const string LeaveApprove =
        "Leave.Approve";

    public const string PayrollView =
        "Payroll.View";

    public const string PayrollCalculate =
        "Payroll.Calculate";

    public const string PayrollClose =
        "Payroll.Close";

    public const string PayrollExport =
        "Payroll.Export";

    public const string ReportView =
        "Report.View";

    public const string ReportExport =
        "Report.Export";

    public const string AccountCreate =
        "Account.Create";

    public const string AccountEdit =
        "Account.Edit";

    public const string AccountLock =
        "Account.Lock";

    public const string AccountAssignRole =
        "Account.AssignRole";

    public const string SettingsManage =
        "Settings.Manage";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(
            new[]
            {
                EmployeeView,
                EmployeeCreate,
                EmployeeEdit,
                EmployeeManageLifecycle,
                AttendanceView,
                AttendanceEdit,
                AttendanceApprove,
                LeaveView,
                LeaveApprove,
                PayrollView,
                PayrollCalculate,
                PayrollClose,
                PayrollExport,
                ReportView,
                ReportExport,
                AccountCreate,
                AccountEdit,
                AccountLock,
                AccountAssignRole,
                SettingsManage
            },
            StringComparer.Ordinal);
}
