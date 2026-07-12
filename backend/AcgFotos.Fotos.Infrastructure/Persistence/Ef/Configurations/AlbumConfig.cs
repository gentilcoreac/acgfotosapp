using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

public class AlbumConfig : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("fot_Albumes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreAlumno).HasMaxLength(150).IsRequired();

        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Curso).WithMany(c => c.Albumes)
               .HasForeignKey(x => x.CursoId).OnDelete(DeleteBehavior.Cascade);
    }
}
