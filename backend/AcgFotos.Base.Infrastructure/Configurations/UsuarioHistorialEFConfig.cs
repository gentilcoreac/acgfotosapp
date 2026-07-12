using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class UsuarioHistorialEFConfig : IEntityTypeConfiguration<UsuarioHistorial>
    {
        public void Configure(EntityTypeBuilder<UsuarioHistorial> builder)
        {
            builder.ToTable("gen_UsuariosHistorial");
            builder.Property(t => t.Periodo).IsRequired().HasMaxLength(450);
            builder.Property(t => t.FechaLastLogin).IsRequired();
            builder.Property(t => t.UsuarioId).IsRequired();
            builder.Property(t => t.TenantId).IsRequired();

            // Índice para el filtro del reporte de usuarios activos (por TenantId + Periodo).
            builder.HasIndex(t => new { t.TenantId, t.Periodo })
                   .HasDatabaseName("IX_gen_UsuariosHistorial_TenantId_Periodo");

            // No se declara relación/FK hacia gen_Usuarios a propósito: UsuarioId queda como columna escalar.
            // El historial de logins debe sobrevivir al borrado del usuario, por eso tampoco existe la FK física.
            // Los reportes resuelven el usuario con un JOIN explícito.
        }
    }
}
