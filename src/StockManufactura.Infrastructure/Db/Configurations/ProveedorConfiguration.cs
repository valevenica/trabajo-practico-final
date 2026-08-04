using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
    {
        public void Configure(EntityTypeBuilder<Proveedor> builder)
        {
            builder.ToTable("Proveedores");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(x => x.RazonSocial).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Cuit).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Direccion).HasMaxLength(250);
            builder.Property(x => x.Ciudad).HasMaxLength(120);
            builder.Property(x => x.Provincia).HasMaxLength(120);
            builder.Property(x => x.Pais).HasMaxLength(120);
            builder.Property(x => x.Telefono).HasMaxLength(50);
            builder.Property(x => x.Email).HasMaxLength(200);
            builder.Property(x => x.PersonaContacto).HasMaxLength(120);
            builder.Property(x => x.Observaciones).HasMaxLength(500);
            builder.Property(x => x.Activo).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.FechaAlta).IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(x => x.Cuit).IsUnique();
        }
    }
}
