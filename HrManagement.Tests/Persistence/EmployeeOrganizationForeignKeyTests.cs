using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HrManagement.Tests.Persistence;
public sealed class EmployeeOrganizationForeignKeyTests
{
    [Fact]
    public async Task SaveEmployee_WithValidOrganizationReferences_PersistsReferences()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
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
                    departmentId,
                    positionId));

            await dbContext.SaveChangesAsync();
        }

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
    public async Task SaveEmployee_WithUnknownDepartmentId_ThrowsDbUpdateException()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new HrManagementDbContext(options);

        await dbContext.Database
            .EnsureCreatedAsync();

        Guid positionId =
            Guid.NewGuid();

        dbContext.Positions.Add(
            new Position(
                positionId,
                "DEV",
                "Lập trình viên"));

        dbContext.Employees.Add(
            CreateEmployee(
                Guid.NewGuid(), // không có Department này
                positionId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveEmployee_WithUnknownPositionId_ThrowsDbUpdateException()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new HrManagementDbContext(options);

        await dbContext.Database
            .EnsureCreatedAsync();

        Guid departmentId =
            Guid.NewGuid();

        dbContext.Departments.Add(
            new Department(
                departmentId,
                "IT",
                "Công nghệ thông tin"));

        dbContext.Employees.Add(
            CreateEmployee(
                departmentId,
                Guid.NewGuid())); // không có Position này

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteDepartment_WhenReferencedByEmployee_ThrowsDbUpdateException()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        await using (var setupContext =
                     new HrManagementDbContext(options))
        {
            await setupContext.Database
                .EnsureCreatedAsync();

            setupContext.Departments.Add(
                new Department(
                    departmentId,
                    "IT",
                    "Công nghệ thông tin"));

            setupContext.Positions.Add(
                new Position(
                    positionId,
                    "DEV",
                    "Lập trình viên"));

            setupContext.Employees.Add(
                CreateEmployee(
                    departmentId,
                    positionId));

            await setupContext.SaveChangesAsync();
        }

        await using var deleteContext =
            new HrManagementDbContext(options);

        Department department =
            await deleteContext.Departments
                .SingleAsync();

        deleteContext.Departments.Remove(
            department);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletePosition_WhenReferencedByEmployee_ThrowsDbUpdateException()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        await using (var setupContext =
                     new HrManagementDbContext(options))
        {
            await setupContext.Database
                .EnsureCreatedAsync();

            setupContext.Departments.Add(
                new Department(
                    departmentId,
                    "IT",
                    "Công nghệ thông tin"));

            setupContext.Positions.Add(
                new Position(
                    positionId,
                    "DEV",
                    "Lập trình viên"));

            setupContext.Employees.Add(
                CreateEmployee(
                    departmentId,
                    positionId));

            await setupContext.SaveChangesAsync();
        }

        await using var deleteContext =
            new HrManagementDbContext(options);

        Position position =
            await deleteContext.Positions
                .SingleAsync();

        deleteContext.Positions.Remove(
            position);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => deleteContext.SaveChangesAsync());
    }

    private static Employee CreateEmployee(
    Guid? departmentId,
    Guid? positionId)
    {
        return new Employee(
            Guid.NewGuid(),
            $"EMP-{Guid.NewGuid():N}",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Công nghệ thông tin",
            "Lập trình viên",
            EmployeeStatus.Active,
            departmentId: departmentId,
            positionId: positionId);
    }
}
