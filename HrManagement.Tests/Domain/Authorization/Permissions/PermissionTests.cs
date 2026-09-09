using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Domain.Authorization.Permissions;

public sealed class PermissionTests
{
    [Theory]
    [InlineData(PermissionCodes.EmployeeView)]
    [InlineData(PermissionCodes.AttendanceApprove)]
    [InlineData(PermissionCodes.PayrollClose)]
    [InlineData(PermissionCodes.AccountAssignRole)]
    [InlineData(PermissionCodes.SettingsManage)]
    [InlineData(PermissionCodes.DepartmentView)]
    [InlineData(PermissionCodes.PositionManageLifecycle)]
    [InlineData(PermissionCodes.WorkScheduleView)]
    [InlineData(PermissionCodes.WorkScheduleDelete)]
    [InlineData(PermissionCodes.WorkScheduleAssign)]
    [InlineData(PermissionCodes.HolidayExceptionView)]
    [InlineData(PermissionCodes.HolidayExceptionManageLifecycle)]
    [InlineData(PermissionCodes.TimesheetView)]
    [InlineData(PermissionCodes.TimesheetClose)]
    [InlineData(PermissionCodes.OvertimeView)]
    [InlineData(PermissionCodes.OvertimeReview)]
    [InlineData(PermissionCodes.OvertimeCancel)]
    [InlineData(PermissionCodes.PayrollManageCompensation)]
    [InlineData(PermissionCodes.LeaveSubmit)]
    [InlineData(PermissionCodes.LeaveCancel)]
    [InlineData(PermissionCodes.AccountView)]
    [InlineData(PermissionCodes.RoleCreate)]
    [InlineData(PermissionCodes.RoleEdit)]
    public void
        Constructor_WithKnownPermission_CreatesPermission(
            string code)
    {
        var permission =
            new Permission(
                code);

        Assert.Equal(
            code,
            permission.Code);
    }

    [Fact]
    public void
        Constructor_TrimsPermissionCode()
    {
        var permission =
            new Permission(
                $"  {PermissionCodes.ReportView}  ");

        Assert.Equal(
            PermissionCodes.ReportView,
            permission.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown.Permission")]
    [InlineData("employee.view")]
    public void
        Constructor_WithInvalidPermission_Throws(
            string code)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new Permission(
                    code));
    }

    [Fact]
    public void
        All_DoesNotContainDuplicates()
    {
        Assert.Equal(
            PermissionCodes.All.Count,
            PermissionCodes.All
                .Distinct(
                    StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void
        All_ContainsExpectedPermissionCount()
    {
        Assert.Equal(
            50,
            PermissionCodes.All.Count);
    }
}
