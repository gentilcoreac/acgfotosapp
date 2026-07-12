using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class UsuarioEFConfig : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("gen_Usuarios");

            builder.Property(t => t.UserName).HasMaxLength(50).IsRequired();
            builder.Property(t => t.Nombre).IsRequired();
            builder.Property(t => t.Email).IsRequired();
            builder.Property(t => t.FechaCambioClave).IsRequired();
            builder.Property(t => t.Telefono);
            builder.Property(t => t.TenantId).IsRequired();
            builder.HasIndex(x => new { x.UserName }).IsUnique();
            builder.HasMany(d => d.Roles).WithOne(e => e.Usuario).
                                          HasForeignKey(f => f.UsuarioId).
                                          OnDelete(DeleteBehavior.Cascade);

            // FK Usuario.TenantId -> Tenant con DeleteBehavior.Restrict (NO ACTION): borrar un tenant que
            // todavía tiene usuarios queda BLOQUEADO (error 547 -> 400), en vez de dejar usuarios huérfanos
            // (hallazgo #19). La baja real de un tenant es la DESACTIVACIÓN (Activo=false), no el hard-delete.
            // Sin navegación (Tenant no es multi-tenant; Usuario sí) para no cruzar el filtro global.
            builder.HasOne<Tenant>()
                   .WithMany()
                   .HasForeignKey(u => u.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
