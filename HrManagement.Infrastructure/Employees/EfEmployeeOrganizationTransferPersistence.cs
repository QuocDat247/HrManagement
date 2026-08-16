using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmployeeOrganizationTransferPersistence
    : IEmployeeOrganizationTransferPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeOrganizationTransferPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task TransferEmployeeOrganizationAsync(
        Employee employee,
        EmployeeOrganizationAssignment closedAssignment,
        EmployeeOrganizationAssignment newAssignment,
        CancellationToken cancellationToken = default)
    {
        ValidateTransfer(
            employee,
            closedAssignment,
            newAssignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Update(
            employee);

        dbContext.EmployeeOrganizationAssignments.Update(
            closedAssignment);

        dbContext.EmployeeOrganizationAssignments.Add(
            newAssignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateTransfer(
        Employee employee,
        EmployeeOrganizationAssignment closedAssignment,
        EmployeeOrganizationAssignment newAssignment)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        ArgumentNullException.ThrowIfNull(
            closedAssignment);

        ArgumentNullException.ThrowIfNull(
            newAssignment);

        if (closedAssignment.EmployeeId
                != employee.Id
            || newAssignment.EmployeeId
                != employee.Id)
        {
            throw new ArgumentException(
                "Lịch sử điều chuyển không thuộc "
                + "nhân viên được cung cấp.");
        }

        if (closedAssignment.Id
            == newAssignment.Id)
        {
            throw new ArgumentException(
                "Phân công mới phải có mã định danh khác.");
        }

        if (closedAssignment.EmploymentPeriodId
            != newAssignment.EmploymentPeriodId)
        {
            throw new ArgumentException(
                "Điều chuyển phải nằm trong cùng "
                + "giai đoạn làm việc.");
        }

        if (!closedAssignment.EndDate.HasValue)
        {
            throw new ArgumentException(
                "Phân công cũ chưa được kết thúc.");
        }

        if (!newAssignment.IsOpen)
        {
            throw new ArgumentException(
                "Phân công mới phải đang mở.");
        }

        DateOnly expectedStartDate =
            closedAssignment.EndDate.Value
                .AddDays(1);

        if (newAssignment.StartDate
            != expectedStartDate)
        {
            throw new ArgumentException(
                "Ngày bắt đầu phân công mới phải ngay sau "
                + "ngày kết thúc phân công cũ.");
        }

        if (!employee.DepartmentId.HasValue
            || !employee.PositionId.HasValue)
        {
            throw new ArgumentException(
                "Nhân viên chưa có đầy đủ thông tin tổ chức.");
        }

        if (employee.DepartmentId.Value
                != newAssignment.DepartmentId
            || employee.PositionId.Value
                != newAssignment.PositionId)
        {
            throw new ArgumentException(
                "Phân công mới không khớp với "
                + "tổ chức hiện tại của nhân viên.");
        }

        if (closedAssignment.DepartmentId
                == newAssignment.DepartmentId
            && closedAssignment.PositionId
                == newAssignment.PositionId)
        {
            throw new ArgumentException(
                "Điều chuyển phải thay đổi phòng ban, "
                + "chức danh hoặc cả hai.");
        }
    }
}
