using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class RecursoTramoPrecioConfiguration : IEntityTypeConfiguration<RecursoTramoPrecio>
    {
        public void Configure(EntityTypeBuilder<RecursoTramoPrecio> builder)
        {
            builder.ToTable("RecursoTramosPrecio");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Orden).IsRequired();
            builder.Property(x => x.CantidadHasta).HasColumnType("decimal(18,4)");
            builder.Property(x => x.PrecioPorUnidad).HasColumnType("decimal(18,4)").IsRequired();

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
