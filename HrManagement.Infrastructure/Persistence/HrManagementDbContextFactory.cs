using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HrManagement.Infrastructure.Persistence;

public sealed class HrManagementDbContextFactory
    : IDesignTimeDbContextFactory<HrManagementDbContext>
{
    public HrManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<HrManagementDbContext>();

        optionsBuilder.UseSqlite(
            DatabasePath.GetConnectionString());

        return new HrManagementDbContext(
            optionsBuilder.Options);
    }
}
