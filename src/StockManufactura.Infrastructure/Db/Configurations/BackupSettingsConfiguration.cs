using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class BackupSettingsConfiguration : IEntityTypeConfiguration<BackupSettings>
    {
        public void Configure(EntityTypeBuilder<BackupSettings> builder)
        {
            builder.ToTable("BackupSettings");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CarpetaLocal).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Automatico).IsRequired();
            builder.Property(x => x.MantenerUltimasCopias).IsRequired();
            builder.Property(x => x.UltimoBackupAutomatico);
            builder.Property(x => x.IntervaloMinutos).IsRequired();
            builder.Property(x => x.GoogleDriveHabilitado).IsRequired();
            builder.Property(x => x.GoogleDriveFolderId).HasMaxLength(250);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }
