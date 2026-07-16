using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_Fotos</c>. Todas las FKs en Restrict, por dos motivos: (1) borrar en cascada una foto
/// dejaría archivos huérfanos en el storage (originals/derived) — el borrado debe ser un flujo
/// explícito que limpie ambos; (2) Evento→Foto y Evento→Grupo→Foto serían rutas de cascada
/// múltiples que SQL Server rechaza.
/// </summary>
public class FotoConfig : IEntityTypeConfiguration<Foto>
{
    public void Configure(EntityTypeBuilder<Foto> builder)
    {
        builder.ToTable("fot_Fotos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreArchivoOriginal).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ErrorProcesamiento).HasMaxLength(1000);

        builder.HasIndex(x => x.TenantId);
        // La key de storage no debe repetirse jamás (colisión = una familia viendo la foto de otra).
        builder.HasIndex(x => x.StorageKey).IsUnique();

        builder.HasOne(x => x.Evento).WithMany()
               .HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Grupo).WithMany()
               .HasForeignKey(x => x.GrupoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Participante).WithMany()
               .HasForeignKey(x => x.ParticipanteId).OnDelete(DeleteBehavior.Restrict);
    }
}
