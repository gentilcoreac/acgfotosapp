using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

public class CodigoAccesoConfig : IEntityTypeConfiguration<CodigoAcceso>
{
    public void Configure(EntityTypeBuilder<CodigoAcceso> builder)
    {
        builder.ToTable("fot_CodigosAcceso");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo).HasMaxLength(20).IsRequired();

        // El canje busca por código dentro del tenant; único para que un código abra UN álbum.
        builder.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique();

        builder.HasOne(x => x.Album).WithMany(a => a.CodigosAcceso)
               .HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade);
    }
}
