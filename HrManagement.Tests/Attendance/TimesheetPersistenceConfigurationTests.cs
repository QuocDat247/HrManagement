using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HrManagement.Tests.Attendance;

public sealed class TimesheetPersistenceConfigurationTests
{
    [Fact]
    public void Model_MapsTimesheetPeriodToExpectedTable()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType<TimesheetPeriod>(
                dbContext);

        Assert.Equal(
            "TimesheetPeriods",
            entityType.GetTableName());

        IKey primaryKey =
            Assert.IsAssignableFrom<IKey>(
                entityType.FindPrimaryKey());

        Assert.Equal(
            nameof(TimesheetPeriod.Id),
            Assert.Single(
                    primaryKey.Properties)
                .Name);
    }

    [Fact]
    public void Model_TimesheetPeriodHasUniqueYearMonthIndex()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType<TimesheetPeriod>(
                dbContext);

        IIndex index =
            Assert.Single(
                entityType
                    .GetIndexes()
                    .Where(
                        item =>
                            item.GetDatabaseName() ==
                            "UX_TimesheetPeriods_Year_Month"));

        Assert.True(
            index.IsUnique);

        Assert.Equal(
            [
                nameof(TimesheetPeriod.Year),
                nameof(TimesheetPeriod.Month)
            ],
            index.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray());
    }

    [Fact]
    public void Model_MapsMonthlySnapshotToExpectedTableAndUniqueKey()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType<MonthlyTimesheetDaySnapshot>(
                dbContext);

        Assert.Equal(
            "MonthlyTimesheetDaySnapshots",
            entityType.GetTableName());

        IIndex index =
            Assert.Single(
                entityType
                    .GetIndexes()
                    .Where(
                        item =>
                            item.GetDatabaseName() ==
                            "UX_MonthlyTimesheetSnapshots_Period_Employee_Date"));

        Assert.True(
            index.IsUnique);

        Assert.Equal(
            [
                nameof(MonthlyTimesheetDaySnapshot.TimesheetPeriodId),
                nameof(MonthlyTimesheetDaySnapshot.EmployeeId),
                nameof(MonthlyTimesheetDaySnapshot.WorkDate)
            ],
            index.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray());
    }

    [Fact]
    public void Model_MonthlySnapshotHasHistoricalForeignKeys()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType<MonthlyTimesheetDaySnapshot>(
                dbContext);

        IReadOnlyList<IForeignKey> foreignKeys =
            entityType
                .GetForeignKeys()
                .ToArray();

        Assert.Equal(
            3,
            foreignKeys.Count);

        AssertForeignKey(
            foreignKeys,
            typeof(TimesheetPeriod),
            nameof(MonthlyTimesheetDaySnapshot.TimesheetPeriodId));

        AssertForeignKey(
            foreignKeys,
            typeof(AttendanceRecord),
            nameof(MonthlyTimesheetDaySnapshot.AttendanceRecordId));

        AssertForeignKey(
            foreignKeys,
            typeof(Employee),
            nameof(MonthlyTimesheetDaySnapshot.EmployeeId));
    }

    private static void AssertForeignKey(
        IReadOnlyList<IForeignKey> foreignKeys,
        Type principalType,
        string propertyName)
    {
        IForeignKey foreignKey =
            Assert.Single(
                foreignKeys.Where(
                    item =>
                        item.PrincipalEntityType.ClrType ==
                        principalType));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);

        Assert.Equal(
            propertyName,
            Assert.Single(
                    foreignKey.Properties)
                .Name);
    }

    private static IEntityType GetEntityType<TEntity>(
        HrManagementDbContext dbContext)
    {
        return Assert.IsAssignableFrom<IEntityType>(
            dbContext.Model.FindEntityType(
                typeof(TEntity)));
    }

    private static HrManagementDbContext CreateDbContext()
    {
        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(
                    "Data Source=:memory:")
                .Options;

        return new HrManagementDbContext(
            options);
    }
}
