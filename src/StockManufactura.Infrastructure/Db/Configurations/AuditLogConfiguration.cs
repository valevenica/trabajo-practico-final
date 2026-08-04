using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FechaHora).IsRequired();
            builder.Property(x => x.Usuario).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Modulo).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Accion).IsRequired().HasMaxLength(120);
            builder.Property(x => x.Entidad).IsRequired().HasMaxLength(120);
            builder.Property(x => x.IdEntidad).HasMaxLength(120);
            builder.Property(x => x.Descripcion).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.Equipo).HasMaxLength(120);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(x => x.FechaHora);
        }
    }
}
