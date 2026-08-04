using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ResourceCostCalculationConfiguration : IEntityTypeConfiguration<ResourceCostCalculation>
    {
        public void Configure(EntityTypeBuilder<ResourceCostCalculation> builder)
        {
            builder.ToTable("ResourceCostCalculations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FechaCalculo).IsRequired();
            builder.Property(x => x.CotizacionUtilizada).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.CostoEnPesos).IsRequired().HasColumnType("decimal(18,4)");

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
