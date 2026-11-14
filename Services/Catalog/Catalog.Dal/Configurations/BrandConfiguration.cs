using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Country)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(b => b.UpdatedAt);

        builder.HasIndex(b => b.Name)
            .IsUnique();

        builder.HasIndex(b => b.Code)
            .IsUnique();

        // Static seed data for base dictionaries (Criterion 9)
        builder.HasData(
            new { Id = 1, Name = "Motul", Code = "MOTUL", Country = "France", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = 2, Name = "Brembo", Code = "BREMBO", Country = "Italy", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = 3, Name = "Bosch", Code = "BOSCH", Country = "Germany", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = 4, Name = "Toptul", Code = "TOPTUL", Country = "Taiwan", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null }
        );
    }
}
