using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class RecursoConfiguration : IEntityTypeConfiguration<Recurso>
    {
        public void Configure(EntityTypeBuilder<Recurso> builder)
        {
            builder.ToTable("Recursos");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(180);
            builder.Property(x => x.Descripcion).HasMaxLength(500);
            builder.Property(x => x.Categoria).HasMaxLength(120);
            builder.Property(x => x.UnidadMedida).HasMaxLength(50);
            builder.Property(x => x.StockActual).HasColumnType("decimal(18,4)");
            builder.Property(x => x.StockMinimo).HasColumnType("decimal(18,4)");
            builder.Property(x => x.Precio).HasColumnType("decimal(18,4)");
            builder.Property(x => x.Moneda).IsRequired();
            builder.Property(x => x.FechaUltimaActualizacion).IsRequired();
            builder.Property(x => x.Observaciones).HasMaxLength(500);
            builder.Property(x => x.Activo).IsRequired().HasDefaultValue(true);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.ProveedorHabitual)
                .WithMany()
                .HasForeignKey(x => x.ProveedorHabitualId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.Codigo).IsUnique();
        }
    }
}
