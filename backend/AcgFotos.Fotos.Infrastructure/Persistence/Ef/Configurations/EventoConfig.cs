using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_Eventos</c> — raíz del vertical. Grupos y catálogo de tamaños cascadean con el evento;
/// las fotos NO (FK Restrict): borrar un evento con fotos exige borrar las fotos primero, porque
/// además de las filas hay archivos en el storage que alguien tiene que limpiar.
/// </summary>
public class EventoConfig : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("fot_Eventos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LugarOrganizacion).HasMaxLength(200);

        builder.HasIndex(x => x.TenantId);

        // Restrict (no SetNull/Cascade): borrar un perfil u opciones en uso es una acción que el
        // ABM debe rechazar explícitamente, no algo que deba pasar en silencio al borrarlo.
        builder.HasOne(x => x.PerfilMarcaAgua).WithMany()
               .HasForeignKey(x => x.PerfilMarcaAguaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OpcionesPublicacion).WithMany()
               .HasForeignKey(x => x.OpcionesPublicacionId).OnDelete(DeleteBehavior.Restrict);
    }
}
