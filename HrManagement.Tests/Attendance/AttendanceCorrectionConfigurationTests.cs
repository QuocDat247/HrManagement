using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceCorrectionConfigurationTests
{
    [Fact]
    public void Model_MapsAttendanceCorrectionToExpectedTable()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            Assert.IsAssignableFrom<IEntityType>(
                dbContext.Model.FindEntityType(
                    typeof(AttendanceCorrection)));

        Assert.Equal(
            "AttendanceCorrections",
            entityType.GetTableName());

        IKey primaryKey =
            Assert.IsAssignableFrom<IKey>(
                entityType.FindPrimaryKey());

        IProperty idProperty =
            Assert.Single(
                primaryKey.Properties);

        Assert.Equal(
            nameof(AttendanceCorrection.Id),
            idProperty.Name);
    }

    [Fact]
    public void Model_HasUniqueRecordRevisionIndex()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType(
                dbContext);

        IIndex index =
            Assert.Single(
                entityType
                    .GetIndexes()
                    .Where(
                        item =>
                            item.GetDatabaseName() ==
                            "UX_AttendanceCorrections_Record_Revision"));

        Assert.True(
            index.IsUnique);

        Assert.Equal(
            [
                nameof(AttendanceCorrection.AttendanceRecordId),
                nameof(AttendanceCorrection.Revision)
            ],
            index.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray());
    }

    [Fact]
    public void Model_HasTimelineAndEmployeeHistoryIndexes()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType(
                dbContext);

        IIndex timelineIndex =
            Assert.Single(
                entityType
                    .GetIndexes()
                    .Where(
                        item =>
                            item.GetDatabaseName() ==
                            "IX_AttendanceCorrections_Record_Event_Revision"));

        Assert.Equal(
            [
                nameof(AttendanceCorrection.AttendanceRecordId),
                nameof(AttendanceCorrection.AffectedEventId),
                nameof(AttendanceCorrection.Revision)
            ],
            timelineIndex.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray());

        IIndex employeeIndex =
            Assert.Single(
                entityType
                    .GetIndexes()
                    .Where(
                        item =>
                            item.GetDatabaseName() ==
                            "IX_AttendanceCorrections_Employee_CorrectedAtUtc"));

        Assert.Equal(
            [
                nameof(AttendanceCorrection.EmployeeId),
                nameof(AttendanceCorrection.CorrectedAtUtc)
            ],
            employeeIndex.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray());
    }

    [Fact]
    public void Model_HasHistoricalForeignKeysButAffectedEventIdIsNotForeignKey()
    {
        using HrManagementDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetEntityType(
                dbContext);

        IReadOnlyList<IForeignKey> foreignKeys =
            entityType
                .GetForeignKeys()
                .ToArray();

        Assert.Equal(
            2,
            foreignKeys.Count);

        IForeignKey attendanceRecordForeignKey =
            Assert.Single(
                foreignKeys.Where(
                    foreignKey =>
                        foreignKey.PrincipalEntityType.ClrType ==
                        typeof(AttendanceRecord)));

        Assert.Equal(
            DeleteBehavior.Restrict,
            attendanceRecordForeignKey.DeleteBehavior);

        Assert.Equal(
            nameof(AttendanceCorrection.AttendanceRecordId),
            Assert.Single(
                    attendanceRecordForeignKey.Properties)
                .Name);

        IForeignKey employeeForeignKey =
            Assert.Single(
                foreignKeys.Where(
                    foreignKey =>
                        foreignKey.PrincipalEntityType.ClrType ==
                        typeof(Employee)));

        Assert.Equal(
            DeleteBehavior.Restrict,
            employeeForeignKey.DeleteBehavior);

        Assert.Equal(
            nameof(AttendanceCorrection.EmployeeId),
            Assert.Single(
                    employeeForeignKey.Properties)
                .Name);

        Assert.DoesNotContain(
            foreignKeys,
            foreignKey =>
                foreignKey.Properties.Any(
                    property =>
                        property.Name ==
                        nameof(AttendanceCorrection.AffectedEventId)));
    }

    private static IEntityType GetEntityType(
        HrManagementDbContext dbContext)
    {
        return Assert.IsAssignableFrom<IEntityType>(
            dbContext.Model.FindEntityType(
                typeof(AttendanceCorrection)));
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
