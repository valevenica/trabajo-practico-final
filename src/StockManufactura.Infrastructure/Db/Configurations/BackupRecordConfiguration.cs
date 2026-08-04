using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
    {
        public void Configure(EntityTypeBuilder<BackupRecord> builder)
        {
            builder.ToTable("BackupRecords");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FechaHora).IsRequired();
            builder.Property(x => x.Tipo).IsRequired().HasMaxLength(50);
            builder.Property(x => x.RutaArchivo).IsRequired().HasMaxLength(500);
            builder.Property(x => x.TamanoBytes).IsRequired();
            builder.Property(x => x.Exitoso).IsRequired();
            builder.Property(x => x.Mensaje).HasMaxLength(1000);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }
}
