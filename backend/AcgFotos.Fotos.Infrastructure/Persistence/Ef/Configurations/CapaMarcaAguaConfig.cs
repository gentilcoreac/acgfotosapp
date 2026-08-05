using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_CapasMarcaAgua</c>. Cascade con el perfil (mismo patrón que <c>TamanoPrecio</c>→<c>Evento</c>):
/// una capa no existe sin su perfil, y ambos se borran en el mismo flujo explícito del ABM, que
/// limpia el asset del storage antes (igual que <c>Foto.EliminarAsync</c>).
/// </summary>
public class CapaMarcaAguaConfig : IEntityTypeConfiguration<CapaMarcaAgua>
{
    public void Configure(EntityTypeBuilder<CapaMarcaAgua> builder)
    {
        builder.ToTable("fot_CapasMarcaAgua");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId);
        // La key de storage no debe repetirse (mismo motivo que Foto.StorageKey).
        builder.HasIndex(x => x.StorageKey).IsUnique();

        builder.HasOne(x => x.PerfilMarcaAgua).WithMany(p => p.Capas)
               .HasForeignKey(x => x.PerfilMarcaAguaId).OnDelete(DeleteBehavior.Cascade);
    }
}
