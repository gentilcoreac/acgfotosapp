using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class GrupoRolEFConfig : IEntityTypeConfiguration<GrupoRol> {
        public void Configure(EntityTypeBuilder<GrupoRol> builder) {

            builder.ToTable("gen_GrupoRoles");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.GrupoId).IsRequired();
            builder.Property(p => p.RolId).IsRequired();

            builder.HasOne(p => p.Grupo)
                   .WithMany(b => b.GrupoRoles)
                   .HasForeignKey(b => b.GrupoId)
                   .OnDelete(DeleteBehavior.Cascade);

            // FK a Rol sin navegación inversa (no hace falta tocar la entidad Rol). Mismo patrón
            // de doble cascada que gen_RolPermisos (Rol + Permiso) y gen_UsuarioGrupos (Usuario + Grupo).
            builder.HasOne(p => p.Rol)
                   .WithMany()
                   .HasForeignKey(b => b.RolId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Unicidad de rol por grupo y tenant (evita duplicados; el front manda Id=0).
            builder.HasIndex(x => new { x.TenantId, x.GrupoId, x.RolId })
                .IsUnique()
                .HasDatabaseName("uk_tenant_grupo_rol");
        }
    }
}
