using HrManagement.Domain.Organization.Positions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

public sealed class PositionConfiguration
    : IEntityTypeConfiguration<Position>
{
    public void Configure(
        EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");

        builder.HasKey(position =>
            position.Id);

        builder.Property(position =>
                position.Id)
            .ValueGeneratedNever();

        builder.Property(position =>
                position.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(position =>
                position.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(position =>
                position.IsActive)
            .IsRequired();

        builder.HasIndex(position =>
                position.Code)
            .IsUnique()
            .HasDatabaseName(
                "UX_Positions_Code");
    }
}
