using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_OpcionesPublicacion</c>. Igual criterio que <see cref="PerfilMarcaAguaConfig"/> para
/// "a lo sumo un default por tenant": lo sostiene el AppService, no un índice filtrado.
/// </summary>
public class OpcionesPublicacionConfig : IEntityTypeConfiguration<OpcionesPublicacion>
{
    public void Configure(EntityTypeBuilder<OpcionesPublicacion> builder)
    {
        builder.ToTable("fot_OpcionesPublicacion");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
