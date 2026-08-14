using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Organization.Assignments;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Organization.Assignments;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static HrManagement.Tests.Employees.EfEmploymentHistoryRepositoryTests;

namespace HrManagement.Tests.Infrastructure.Organization.Assignments;
public sealed class EfEmployeeOrganizationBackfillServiceTests
{
    [Fact]
    public async Task BackfillAsync_WhenNamesMatch_AssignsBothReferences()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "IT",
                    "Công nghệ thông tin"));

            dbContext.Positions.Add(
                new Position(
                    positionId,
                    "DEV",
                    "Lập trình viên"));

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "Công nghệ thông tin",
                    "Lập trình viên"));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmployeeOrganizationBackfillService(
                new TestDbContextFactory(options));

        EmployeeOrganizationBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(1, result.ScannedEmployees);
        Assert.Equal(1, result.UpdatedEmployees);

        Assert.Equal(
            1,
            result.AssignedDepartmentReferences);

        Assert.Equal(
            1,
            result.AssignedPositionReferences);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee employee =
            await verificationContext.Employees
                .SingleAsync();

        Assert.Equal(
            departmentId,
            employee.DepartmentId);

        Assert.Equal(
            positionId,
            employee.PositionId);

        // Legacy display strings vẫn còn nguyên.
        Assert.Equal(
            "Công nghệ thông tin",
            employee.Department);

        Assert.Equal(
            "Lập trình viên",
            employee.Position);
    }

    [Fact]
    public async Task BackfillAsync_WhenCodesMatch_AssignsBothReferences()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "IT",
                    "Công nghệ thông tin"));

            dbContext.Positions.Add(
                new Position(
                    positionId,
                    "DEV",
                    "Lập trình viên"));

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    " it ",
                    " dev "));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmployeeOrganizationBackfillService(
                new TestDbContextFactory(options));

        EmployeeOrganizationBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.ScannedEmployees);

        Assert.Equal(
            1,
            result.UpdatedEmployees);

        Assert.Equal(
            1,
            result.AssignedDepartmentReferences);

        Assert.Equal(
            1,
            result.AssignedPositionReferences);

        Assert.Equal(
            0,
            result.UnresolvedDepartmentReferences);

        Assert.Equal(
            0,
            result.UnresolvedPositionReferences);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee employee =
            await verificationContext.Employees
                .SingleAsync();

        Assert.Equal(
            departmentId,
            employee.DepartmentId);

        Assert.Equal(
            positionId,
            employee.PositionId);
    }

    [Fact]
    public async Task BackfillAsync_WhenOnlyOneReferenceCanBeResolved_AssignsOnlyResolvedReference()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "HR",
                    "Nhân sự"));

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "Nhân sự",
                    "Chức danh legacy không tồn tại"));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmployeeOrganizationBackfillService(
                new TestDbContextFactory(options));

        EmployeeOrganizationBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.ScannedEmployees);

        // Employee vẫn được update vì DepartmentId
        // đã resolve thành công.
        Assert.Equal(
            1,
            result.UpdatedEmployees);

        Assert.Equal(
            1,
            result.AssignedDepartmentReferences);

        Assert.Equal(
            0,
            result.AssignedPositionReferences);

        Assert.Equal(
            0,
            result.UnresolvedDepartmentReferences);

        Assert.Equal(
            1,
            result.UnresolvedPositionReferences);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee employee =
            await verificationContext.Employees
                .SingleAsync();

        Assert.Equal(
            departmentId,
            employee.DepartmentId);

        Assert.Null(
            employee.PositionId);

        // Legacy string không bị mất.
        Assert.Equal(
            "Chức danh legacy không tồn tại",
            employee.Position);
    }

    [Fact]
    public async Task BackfillAsync_WhenReferenceIsAmbiguous_DoesNotAssignAmbiguousReference()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid existingPositionId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.AddRange(
                new Department(
                    Guid.NewGuid(),
                    "TECH-A",
                    "Công nghệ"),

                new Department(
                    Guid.NewGuid(),
                    "TECH-B",
                    "Công nghệ"));

            dbContext.Positions.Add(
                new Position(
                    existingPositionId,
                    "DEV",
                    "Lập trình viên"));

            // PositionId đã có sẵn để test này
            // chỉ tập trung vào Department ambiguity.
            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "Công nghệ",
                    "Lập trình viên",
                    positionId:
                        existingPositionId));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmployeeOrganizationBackfillService(
                new TestDbContextFactory(options));

        EmployeeOrganizationBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.ScannedEmployees);

        // Không có field nào thay đổi.
        Assert.Equal(
            0,
            result.UpdatedEmployees);

        Assert.Equal(
            0,
            result.AssignedDepartmentReferences);

        Assert.Equal(
            1,
            result.AmbiguousDepartmentReferences);

        Assert.Equal(
            0,
            result.UnresolvedDepartmentReferences);

        // PositionId đã tồn tại nên backfill
        // không đụng tới nó.
        Assert.Equal(
            0,
            result.AssignedPositionReferences);

        Assert.Equal(
            0,
            result.UnresolvedPositionReferences);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee employee =
            await verificationContext.Employees
                .SingleAsync();

        Assert.Null(
            employee.DepartmentId);

        Assert.Equal(
            existingPositionId,
            employee.PositionId);
    }

    [Fact]
    public async Task BackfillAsync_WhenRunAgain_IsIdempotent()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "FIN",
                    "Tài chính"));

            dbContext.Positions.Add(
                new Position(
                    positionId,
                    "ACC",
                    "Kế toán viên"));

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "Tài chính",
                    "Kế toán viên"));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmployeeOrganizationBackfillService(
                new TestDbContextFactory(options));

        EmployeeOrganizationBackfillResult firstResult =
            await service.BackfillAsync();

        EmployeeOrganizationBackfillResult secondResult =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            firstResult.UpdatedEmployees);

        Assert.Equal(
            1,
            firstResult.AssignedDepartmentReferences);

        Assert.Equal(
            1,
            firstResult.AssignedPositionReferences);

        // Lần thứ hai không được ghi lại gì.
        Assert.Equal(
            1,
            secondResult.ScannedEmployees);

        Assert.Equal(
            0,
            secondResult.UpdatedEmployees);

        Assert.Equal(
            0,
            secondResult.AssignedDepartmentReferences);

        Assert.Equal(
            0,
            secondResult.AssignedPositionReferences);

        Assert.Equal(
            0,
            secondResult.UnresolvedDepartmentReferences);

        Assert.Equal(
            0,
            secondResult.UnresolvedPositionReferences);

        Assert.Equal(
            0,
            secondResult.AmbiguousDepartmentReferences);

        Assert.Equal(
            0,
            secondResult.AmbiguousPositionReferences);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee employee =
            await verificationContext.Employees
                .SingleAsync();

        Assert.Equal(
            departmentId,
            employee.DepartmentId);

        Assert.Equal(
            positionId,
            employee.PositionId);
    }

    private static Employee CreateEmployee(
    Guid employeeId,
    string department,
    string position,
    Guid? departmentId = null,
    Guid? positionId = null)
    {
        return new Employee(
            employeeId,
            $"EMP-{employeeId:N}",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            department,
            position,
            EmployeeStatus.Active,
            departmentId: departmentId,
            positionId: positionId);
    }
}
