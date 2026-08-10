using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using HrManagement.Infrastructure.Persistence.Configurations;

namespace HrManagement.Infrastructure.Persistence;

public sealed class HrManagementDbContext : DbContext
{
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
    }
}
