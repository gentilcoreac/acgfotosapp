using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

public class GrupoConfig : IEntityTypeConfiguration<Grupo>
{
    public void Configure(EntityTypeBuilder<Grupo> builder)
    {
        builder.ToTable("fot_Grupos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Evento).WithMany(e => e.Grupos)
               .HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Cascade);
    }
}
