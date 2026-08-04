using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ProductCostSnapshotItemConfiguration : IEntityTypeConfiguration<ProductCostSnapshotItem>
    {
        public void Configure(EntityTypeBuilder<ProductCostSnapshotItem> builder)
        {
            builder.ToTable("ProductCostSnapshotItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CantidadUtilizada).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.PrecioRecurso).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CotizacionUtilizada).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CostoParcial).IsRequired().HasColumnType("decimal(18,4)");

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Snapshot)
                .WithMany()
                .HasForeignKey(x => x.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Recurso)
                .WithMany()
                .HasForeignKey(x => x.RecursoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
