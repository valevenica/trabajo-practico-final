using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class CostoIndirectoConfiguration : IEntityTypeConfiguration<CostoIndirecto>
    {
        public void Configure(EntityTypeBuilder<CostoIndirecto> builder)
        {
            builder.ToTable("CostosIndirectos");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(x => x.ValorMensual).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.FactorAplicacion).IsRequired().HasColumnType("decimal(18,6)");
            builder.Property(x => x.Activo).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.Observaciones).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }
}
