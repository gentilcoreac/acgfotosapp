using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

public class TamanoPrecioConfig : IEntityTypeConfiguration<TamanoPrecio>
{
    public void Configure(EntityTypeBuilder<TamanoPrecio> builder)
    {
        builder.ToTable("fot_TamanosPrecios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PrecioUnitario).HasPrecision(10, 2);

        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Evento).WithMany(e => e.TamanosPrecios)
               .HasForeignKey(x => x.EventoId).OnDelete(DeleteBehavior.Cascade);
    }
}
