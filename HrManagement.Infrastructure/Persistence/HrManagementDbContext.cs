using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using HrManagement.Infrastructure.Persistence.Configurations;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Infrastructure.Persistence;

public sealed class HrManagementDbContext : DbContext
{
    public DbSet<Position> Positions =>
    Set<Position>();
    public DbSet<EmploymentPeriod> EmploymentPeriods =>
    Set<EmploymentPeriod>();
    public DbSet<Department> Departments =>
    Set<Department>();

    public HrManagementDbContext(
        DbContextOptions<HrManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(
            new EmploymentPeriodConfiguration());
        modelBuilder.ApplyConfiguration(
            new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(
            new PositionConfiguration());
    }
}
