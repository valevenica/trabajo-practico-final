using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
    {
        public void Configure(EntityTypeBuilder<ExchangeRate> builder)
        {
            builder.ToTable("ExchangeRates");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Valor).IsRequired().HasColumnType("decimal(18,4)");
            builder.Property(x => x.Fecha).IsRequired();
            builder.Property(x => x.Fuente).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Usuario).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Automatica).IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }
}
