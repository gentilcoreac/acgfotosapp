using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class RolesPermisoEFConfig : IEntityTypeConfiguration<RolPermiso>
    {
        public void Configure(EntityTypeBuilder<RolPermiso> builder)
        {
            builder.ToTable("gen_RolPermisos");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.RolId).IsRequired();
            builder.Property(p => p.PermisoId).IsRequired();

            builder.HasOne(p => p.Rol)
                   .WithMany(b => b.RolPermisos).HasForeignKey(b => b.RolId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(p => p.Permiso)
                   .WithMany(b => b.RolPermisos).HasForeignKey(b => b.PermisoId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
