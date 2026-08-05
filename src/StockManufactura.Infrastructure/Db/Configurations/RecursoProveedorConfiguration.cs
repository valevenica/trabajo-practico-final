using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class RecursoProveedorConfiguration : IEntityTypeConfiguration<RecursoProveedor>
    {
        public void Configure(EntityTypeBuilder<RecursoProveedor> builder)
        {
            builder.ToTable("RecursoProveedores");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Precio).HasColumnType("decimal(18,4)");
            builder.Property(x => x.Observaciones).HasMaxLength(500);

            builder.HasOne(x => x.Recurso)
                .WithMany()
                .HasForeignKey(x => x.RecursoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Proveedor)
                .WithMany()
                .HasForeignKey(x => x.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.RecursoId, x.ProveedorId }).IsUnique();
        }
    }
}
