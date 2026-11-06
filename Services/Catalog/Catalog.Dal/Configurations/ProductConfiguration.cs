using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Products_CostPrice", "[CostPrice] >= 0");
            t.HasCheckConstraint("CK_Products_Stock", "[Stock] >= 0");
            t.HasCheckConstraint("CK_Products_WeightKg", "[WeightKg] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.BrandId)
            .IsRequired();

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.CostPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.WeightKg)
            .IsRequired()
            .HasColumnType("decimal(8,3)");

        builder.Property(p => p.Stock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(p => p.UpdatedAt);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.HasIndex(p => new { p.BrandId, p.Price });

        // Navigation configuration
        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 relationship with ProductDetails where ProductDetails.ProductId is both PK and FK (Criterion 3)
        builder.HasOne(p => p.Details)
            .WithOne(d => d.Product)
            .HasForeignKey<ProductDetails>(d => d.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:N relationship with ProductImage with cascade delete (Criterion 3)
        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // M:N relationship with Categories via intermediate ProductCategory entity (Criterion 3)
        builder.HasMany(p => p.ProductCategories)
            .WithOne(pc => pc.Product)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Field navigation mappings for encapsulated collections
        builder.Navigation(p => p.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(p => p.ProductCategories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
