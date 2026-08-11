using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Db.Configurations
{
    public class RolConfiguration : IEntityTypeConfiguration<Rol>
    {
        private const string PermisosSeparator = ";";

        public void Configure(EntityTypeBuilder<Rol> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.Descripcion)
                .HasMaxLength(250);

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            builder.Property(r => r.IsDeleted)
                .HasDefaultValue(false);

            // Permisos is a computed read-only wrapper over the private _permisos field;
            // map the field directly so permission changes are actually persisted.
            builder.Ignore(r => r.Permisos);

            var permisosProperty = builder.Property<List<string>>("_permisos")
                .HasField("_permisos")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("Permisos")
                .HasMaxLength(2000)
                .HasConversion(
                    v => string.Join(PermisosSeparator, v),
                    v => string.IsNullOrEmpty(v)
                        ? new List<string>()
                        : v.Split(PermisosSeparator, System.StringSplitOptions.RemoveEmptyEntries).ToList())
                .IsRequired();

            permisosProperty.Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
                c => c.Aggregate(0, (hash, v) => System.HashCode.Combine(hash, v.GetHashCode())),
                c => c.ToList()));
        }
    }
}
