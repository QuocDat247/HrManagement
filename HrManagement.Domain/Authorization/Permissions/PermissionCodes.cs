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

    public const string DepartmentView =
        "Department.View";

    public const string DepartmentCreate =
        "Department.Create";

    public const string DepartmentEdit =
        "Department.Edit";

    public const string DepartmentManageLifecycle =
        "Department.ManageLifecycle";

    public const string PositionView =
        "Position.View";

    public const string PositionCreate =
        "Position.Create";

    public const string PositionEdit =
        "Position.Edit";

    public const string PositionManageLifecycle =
        "Position.ManageLifecycle";

    public const string WorkScheduleView =
        "WorkSchedule.View";

    public const string WorkScheduleCreate =
        "WorkSchedule.Create";

    public const string WorkScheduleEdit =
        "WorkSchedule.Edit";

    public const string WorkScheduleManageLifecycle =
        "WorkSchedule.ManageLifecycle";

    public const string WorkScheduleDelete =
        "WorkSchedule.Delete";

    public const string WorkScheduleAssign =
        "WorkSchedule.Assign";

    public const string HolidayExceptionView =
        "HolidayException.View";

    public const string HolidayExceptionCreate =
        "HolidayException.Create";

    public const string HolidayExceptionEdit =
        "HolidayException.Edit";

    public const string HolidayExceptionManageLifecycle =
        "HolidayException.ManageLifecycle";

    public const string TimesheetView =
        "Timesheet.View";

    public const string TimesheetClose =
        "Timesheet.Close";

    public const string OvertimeView =
        "Overtime.View";

    public const string OvertimeSubmit =
        "Overtime.Submit";

    public const string OvertimeReview =
        "Overtime.Review";

    public const string OvertimeCancel =
        "Overtime.Cancel";

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

    public const string PayrollManageCompensation =
        "Payroll.ManageCompensation";

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
                DepartmentView,
                DepartmentCreate,
                DepartmentEdit,
                DepartmentManageLifecycle,
                PositionView,
                PositionCreate,
                PositionEdit,
                PositionManageLifecycle,
                WorkScheduleView,
                WorkScheduleCreate,
                WorkScheduleEdit,
                WorkScheduleManageLifecycle,
                WorkScheduleDelete,
                WorkScheduleAssign,
                HolidayExceptionView,
                HolidayExceptionCreate,
                HolidayExceptionEdit,
                HolidayExceptionManageLifecycle,
                TimesheetView,
                TimesheetClose,
                OvertimeView,
                OvertimeSubmit,
                OvertimeReview,
                OvertimeCancel,
                AttendanceView,
                AttendanceEdit,
                AttendanceApprove,
                LeaveView,
                LeaveApprove,
                PayrollView,
                PayrollCalculate,
                PayrollClose,
                PayrollExport,
                PayrollManageCompensation,
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
