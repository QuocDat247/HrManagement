using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Application.Organization.Departments;
using HrManagement.Infrastructure.Organization.Departments;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static HrManagement.Tests.Employees.EfEmploymentHistoryRepositoryTests;

namespace HrManagement.Tests.Domain.Organization.Departments;
public sealed class EfDepartmentRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsDepartment()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var repository =
            new EfDepartmentRepository(
                new TestDbContextFactory(options));

        var department =
            new Department(
                Guid.NewGuid(),
                "IT",
                "Công nghệ thông tin");

        await repository.AddAsync(
            department);

        Department? persisted =
            await repository.GetByIdAsync(
                department.Id);

        Assert.NotNull(persisted);

        Assert.Equal(
            department.Id,
            persisted.Id);

        Assert.Equal(
            "IT",
            persisted.Code);

        Assert.Equal(
            "Công nghệ thông tin",
            persisted.Name);

        Assert.True(
            persisted.IsActive);
    }

    [Fact]
    public async Task GetByCodeAsync_WithUnnormalizedCode_ReturnsDepartment()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    Guid.NewGuid(),
                    "HR",
                    "Nhân sự"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfDepartmentRepository(
                new TestDbContextFactory(options));

        Department? department =
            await repository.GetByCodeAsync(
                " hr ");

        Assert.NotNull(department);

        Assert.Equal(
            "HR",
            department.Code);
    }

    [Fact]
    public async Task UpdateAsync_PersistsDepartmentChanges()
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

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "OPS",
                    "Vận hành"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfDepartmentRepository(
                new TestDbContextFactory(options));

        var updatedDepartment =
            new Department(
                departmentId,
                "OPERATIONS",
                "Khối Vận hành",
                false);

        await repository.UpdateAsync(
            updatedDepartment);

        Department? persisted =
            await repository.GetByIdAsync(
                departmentId);

        Assert.NotNull(persisted);

        Assert.Equal(
            "OPERATIONS",
            persisted.Code);

        Assert.Equal(
            "Khối Vận hành",
            persisted.Name);

        Assert.False(
            persisted.IsActive);
    }

    [Fact]
    public async Task AddAsync_WhenCodeAlreadyExists_ThrowsDbUpdateException()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var repository =
            new EfDepartmentRepository(
                new TestDbContextFactory(options));

        await repository.AddAsync(
            new Department(
                Guid.NewGuid(),
                "FIN",
                "Tài chính"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(
                new Department(
                    Guid.NewGuid(),
                    "FIN",
                    "Tài chính khác")));
    }
}
