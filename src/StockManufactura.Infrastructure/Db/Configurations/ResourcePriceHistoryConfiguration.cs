using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ResourcePriceHistoryConfiguration : IEntityTypeConfiguration<ResourcePriceHistory>
    {
        public void Configure(EntityTypeBuilder<ResourcePriceHistory> builder)
        {
            builder.ToTable("ResourcePriceHistory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Fecha).IsRequired();
            builder.Property(x => x.Usuario).IsRequired().HasMaxLength(120);
            builder.Property(x => x.PrecioAnterior).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.PrecioNuevo).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.Moneda).IsRequired();
            builder.Property(x => x.CotizacionUtilizada).HasColumnType("decimal(18,4)");
            builder.Property(x => x.PrecioEquivalentePesos).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.MotivoCambio).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Observaciones).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Recurso)
                .WithMany()
                .HasForeignKey(x => x.RecursoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.RecursoId);
        }
    }
}
