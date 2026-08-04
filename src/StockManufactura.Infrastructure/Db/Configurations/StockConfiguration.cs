using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
    {
        public void Configure(EntityTypeBuilder<Stock> builder)
        {
            builder.ToTable("Stocks");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductoId).IsRequired();
            builder.Property(x => x.UbicacionId).IsRequired();
            builder.Property(x => x.CantidadDisponible).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CantidadReservada).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.UltimaActualizacion).IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ProductoId, x.UbicacionId }).IsUnique();
        }
    }
}
