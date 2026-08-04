using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ProductCostHistoryConfiguration : IEntityTypeConfiguration<ProductCostHistory>
    {
        public void Configure(EntityTypeBuilder<ProductCostHistory> builder)
        {
            builder.ToTable("ProductCostHistory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Fecha).IsRequired();
            builder.Property(x => x.Usuario).IsRequired().HasMaxLength(120);
            builder.Property(x => x.CostoAnterior).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CostoNuevo).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.PrecioSugeridoAnterior).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.PrecioSugeridoNuevo).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.MargenUtilizado).IsRequired().HasColumnType("decimal(18,6)");
            builder.Property(x => x.CotizacionUtilizada).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.MotivoRecalculo).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Observaciones).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ProductId);
        }
    }
