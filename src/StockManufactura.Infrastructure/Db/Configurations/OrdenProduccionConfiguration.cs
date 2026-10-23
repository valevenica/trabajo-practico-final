using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class OrdenProduccionConfiguration : IEntityTypeConfiguration<OrdenProduccion>
    {
        public void Configure(EntityTypeBuilder<OrdenProduccion> builder)
        {
            builder.ToTable("OrdenesProduccion");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired().HasMaxLength(50);
            builder.Property(x => x.CantidadPlaneada).HasColumnType("decimal(18,4)");
            builder.Property(x => x.CantidadProducida).HasColumnType("decimal(18,4)");
            builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
            builder.Property(x => x.Observaciones).HasMaxLength(2000);
            builder.Property(x => x.FechaInicio);
            builder.Property(x => x.FechaFin);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(x => x.Codigo).IsUnique();
            builder.HasIndex(x => x.ProductoId);

            builder.HasOne<Producto>()
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cliente opcional: sin cliente, el pedido es una venta a consumidor final. Un cliente con pedidos no se
            // elimina, se desactiva.
            builder.HasIndex(x => x.ClienteId);
            builder.HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(x => x.ClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
