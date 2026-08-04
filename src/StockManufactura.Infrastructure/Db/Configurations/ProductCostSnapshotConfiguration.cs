using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ProductCostSnapshotConfiguration : IEntityTypeConfiguration<ProductCostSnapshot>
    {
        public void Configure(EntityTypeBuilder<ProductCostSnapshot> builder)
        {
            builder.ToTable("ProductCostSnapshots");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Fecha).IsRequired();
            builder.Property(x => x.CostoTotal).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CotizacionUtilizada).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CostoFinal).IsRequired().HasColumnType("decimal(18,4)");

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
