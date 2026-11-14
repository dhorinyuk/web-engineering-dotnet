using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductDetailsConfiguration : IEntityTypeConfiguration<ProductDetails>
{
    public void Configure(EntityTypeBuilder<ProductDetails> builder)
    {
        builder.ToTable("ProductDetails", t =>
        {
            t.HasCheckConstraint("CK_ProductDetails_WarrantyMonths", "[WarrantyMonths] >= 0");
        });

        // 1:1 relationship where PK is simultaneously FK (Criterion 3)
        builder.HasKey(d => d.ProductId);

        builder.Property(d => d.ProductId)
            .ValueGeneratedNever();

        builder.Property(d => d.TechnicalDescription)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(d => d.WarrantyMonths)
            .IsRequired()
            .HasDefaultValue(12);

        builder.Property(d => d.CountryOfOrigin)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(d => d.Dimensions)
            .HasMaxLength(100);

        builder.Property(d => d.InstallationGuideUrl)
            .HasMaxLength(255);

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(d => d.UpdatedAt);

        builder.HasOne(d => d.Product)
            .WithOne(p => p.Details)
            .HasForeignKey<ProductDetails>(d => d.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
