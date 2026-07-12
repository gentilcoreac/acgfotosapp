using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class GrupoEFConfig : IEntityTypeConfiguration<Grupo> {
        public void Configure(EntityTypeBuilder<Grupo> builder) {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_Grupos");
            builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();

            // Índice para el filtro global multi-tenant (toda query filtra por TenantId).
            builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_grupo_tenant");

            // Borrar el grupo borra sus filas de membresía (gen_UsuarioGrupos).
            // NOTA al portar a Budgeting/Raymos: allá Grupo también es referenciado por BPF
            // (BudBPFUserGroup) y ciclos de aprobación (BudApprovalCycleUserGroupNode); esas FKs
            // deben pasar a OnDelete(Cascade) (BudGroupDAP ya cascada hoy) para unificar las 4 —
            // decisión de negocio: borrar el grupo lo quita de esos subsistemas. Acá no existen.
            builder.HasMany(d => d.UsuarioGrupos).WithOne(e => e.Grupo).HasForeignKey(f => f.GrupoId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
