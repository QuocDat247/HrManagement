using System.Collections;
using System.Reflection;
using HrManagement.Domain.Auditing;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HrManagement.Tests.Auditing;

public sealed class AuditSensitiveDataGuardrailTests
{
    private static readonly string[]
        ForbiddenPropertyNames =
        [
            "Payload",
            "OldValues",
            "NewValues",
            "OldValue",
            "NewValue",
            "Changes",
            "Details",
            "Description",
            "DocumentNumber",
            "AddressLine",
            "PhoneNumber",
            "Email",
            "DateOfBirth",
            "FullName",
            "PreferredName",
            "Nationality",
            "PlaceOfBirth",
            "IssuingAuthority",
            "PlaceOfIssue",
            "IssuingCountry"
        ];

    [Fact]
    public void AuditEntry_PublicContract_DoesNotExposeSensitiveOrFreeFormPayloadFields()
    {
        string[] propertyNames =
            typeof(AuditEntry)
                .GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance)
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        foreach (string forbiddenName
                 in ForbiddenPropertyNames)
        {
            bool exists =
                propertyNames.Any(
                    propertyName =>
                        string.Equals(
                            propertyName,
                            forbiddenName,
                            StringComparison.OrdinalIgnoreCase));

            Assert.False(
                exists,
                $"AuditEntry không được chứa property '{forbiddenName}'.");
        }
    }

    [Fact]
    public void AuditEntry_PublicContract_DoesNotExposeGenericPayloadContainers()
    {
        PropertyInfo[] properties =
            typeof(AuditEntry)
                .GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance);

        foreach (PropertyInfo property
                 in properties)
        {
            Type propertyType =
                property.PropertyType;

            bool isGenericPayloadContainer =
                propertyType == typeof(object)
                || propertyType == typeof(byte[])
                || typeof(IDictionary)
                    .IsAssignableFrom(
                        propertyType)
                || (
                    propertyType != typeof(string)
                    && typeof(IEnumerable)
                        .IsAssignableFrom(
                            propertyType))
                || (
                    propertyType.FullName
                        ?.StartsWith(
                            "System.Text.Json.",
                            StringComparison.Ordinal)
                    == true);

            Assert.False(
                isGenericPayloadContainer,
                $"AuditEntry không được dùng '{property.Name}' như vùng chứa payload tự do.");
        }
    }

    [Fact]
    public void AuditEntry_EfModel_DoesNotMapSensitivePayloadOrShadowProperties()
    {
        using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        using var dbContext =
            new HrManagementDbContext(
                options);

        var entityType =
            dbContext.Model
                .FindEntityType(
                    typeof(AuditEntry));

        Assert.NotNull(
            entityType);

        var mappedProperties =
            entityType!
                .GetProperties()
                .ToArray();

        foreach (string forbiddenName
                 in ForbiddenPropertyNames)
        {
            bool exists =
                mappedProperties.Any(
                    property =>
                        string.Equals(
                            property.Name,
                            forbiddenName,
                            StringComparison.OrdinalIgnoreCase));

            Assert.False(
                exists,
                $"AuditEntries không được map column '{forbiddenName}'.");
        }

        Assert.DoesNotContain(
            mappedProperties,
            property =>
                property.IsShadowProperty());
    }
}
