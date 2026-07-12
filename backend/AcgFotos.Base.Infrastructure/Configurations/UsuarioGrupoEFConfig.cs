using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class UsuarioGrupoEFConfig : IEntityTypeConfiguration<UsuarioGrupo> {
        public void Configure(EntityTypeBuilder<UsuarioGrupo> builder) {

            builder.ToTable("gen_UsuarioGrupos");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.UsuarioId).IsRequired();
            builder.Property(p => p.GrupoId).IsRequired();

            builder.HasOne(p => p.Usuario)
                   .WithMany(b => b.UsuarioGrupos)
                   .HasForeignKey(b => b.UsuarioId)
                   .OnDelete(DeleteBehavior.Cascade);

            // La relación con Grupo la define GrupoEFConfig (HasMany.WithOne) para no duplicarla.

            // Unicidad de membresía por tenant: evita agregar el mismo usuario dos veces al grupo
            // (el front manda todos los miembros con Id=0).
            builder.HasIndex(x => new { x.TenantId, x.GrupoId, x.UsuarioId })
                .IsUnique()
                .HasDatabaseName("uk_tenant_grupo_usuario");

            // Performance de la vista vw_UsuarioRolesEfectivos: su branch de grupos filtra por
            // UsuarioId, pero el índice único de arriba arranca por (TenantId, GrupoId) → no podía
            // hacer seek por UsuarioId. Este índice arranca por UsuarioId e incluye GrupoId/TenantId
            // → seek por UsuarioId + covering del join a gen_GrupoRoles (sin key lookups).
            builder.HasIndex(x => x.UsuarioId)
                .HasDatabaseName("ix_usuariogrupo_usuario")
                .IncludeProperties(x => new { x.GrupoId, x.TenantId });
        }
    }
}
