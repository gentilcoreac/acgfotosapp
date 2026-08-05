using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_PerfilesMarcaAgua</c>. "A lo sumo un default por tenant" NO se fuerza con un índice
/// filtrado: al marcar un perfil default, la operación de negocio (AppService) tiene que desmarcar
/// el anterior de todos modos, así que la invariante se sostiene ahí (mismo criterio que
/// <c>TipoLicencia.EsDefaultParaNuevoTenant</c>).
/// </summary>
public class PerfilMarcaAguaConfig : IEntityTypeConfiguration<PerfilMarcaAgua>
{
    public void Configure(EntityTypeBuilder<PerfilMarcaAgua> builder)
    {
        builder.ToTable("fot_PerfilesMarcaAgua");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
