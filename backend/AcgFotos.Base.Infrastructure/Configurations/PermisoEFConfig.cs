using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class PermisoEFConfig : IEntityTypeConfiguration<Permiso>
    {
        public void Configure(EntityTypeBuilder<Permiso> builder)
        {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_Permisos");
            builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            builder.Property(x => x.CodigoPermiso).IsRequired();
            builder.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Activo).IsRequired();
            builder.Property(d => d.EsRestringido).IsRequired();
            builder.Property(x => x.PermisoPadreId);
            builder.HasIndex(u => u.PermisoPadreId).IsUnique(false);
            builder.HasOne(x => x.PermisoPadre)
                .WithMany(d => d.PermisosHijos).HasForeignKey(f => f.PermisoPadreId).OnDelete(DeleteBehavior.ClientNoAction);
            builder.Property(x => x.AplicacionId);
            builder.HasIndex(u => u.AplicacionId).IsUnique(false);

            builder.HasMany(d => d.RolPermisos).WithOne(e => e.Permiso).HasForeignKey(f => f.PermisoId).OnDelete(DeleteBehavior.Cascade);

        }
    }
}
