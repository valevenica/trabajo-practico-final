using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(u => u.EsActivo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.UltimoAcceso);

            builder.Property(u => u.RequiereCambioPassword)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.IntentosFallidosLogin)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(u => u.BloqueadoHastaUtc);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired();

            builder.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            builder.HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
