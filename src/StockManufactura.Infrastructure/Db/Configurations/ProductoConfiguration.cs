using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Productos");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(180);
            builder.Property(x => x.Descripcion).HasMaxLength(500);
            builder.Property(x => x.CostoFabricacionActual).HasColumnType("decimal(18,4)");
            builder.Property(x => x.MargenActual).HasColumnType("decimal(18,6)");
            builder.Property(x => x.PrecioSugeridoActual).HasColumnType("decimal(18,4)");
            builder.Property(x => x.Activo).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.FechaUltimoCalculo).IsRequired();
            builder.Property(x => x.Observaciones).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(x => x.Codigo).IsUnique();
        }
    }
