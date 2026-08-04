using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class RecetaProductoItemConfiguration : IEntityTypeConfiguration<RecetaProductoItem>
    {
        public void Configure(EntityTypeBuilder<RecetaProductoItem> builder)
        {
            builder.ToTable("RecetaProductoItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Cantidad).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CostoParcialManual).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.Observaciones).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Recurso)
                .WithMany()
                .HasForeignKey(x => x.RecursoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ProductoId, x.RecursoId }).IsUnique();
        }
    }
}
