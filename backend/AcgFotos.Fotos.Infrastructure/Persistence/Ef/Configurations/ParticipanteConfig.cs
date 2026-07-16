using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

public class ParticipanteConfig : IEntityTypeConfiguration<Participante>
{
    public void Configure(EntityTypeBuilder<Participante> builder)
    {
        builder.ToTable("fot_Participantes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();

        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Grupo).WithMany(c => c.Participantes)
               .HasForeignKey(x => x.GrupoId).OnDelete(DeleteBehavior.Cascade);
    }
}
