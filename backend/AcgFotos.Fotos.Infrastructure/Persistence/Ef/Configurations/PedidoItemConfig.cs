using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_PedidoItems</c>. Los items cascadean con su pedido; Foto y TamanoPrecio en Restrict
/// (un pedido referencia la foto pedida y el tamaño elegido: no pueden desaparecer debajo).
/// </summary>
public class PedidoItemConfig : IEntityTypeConfiguration<PedidoItem>
{
    public void Configure(EntityTypeBuilder<PedidoItem> builder)
    {
        builder.ToTable("fot_PedidoItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrecioUnitarioSnapshot).HasPrecision(10, 2);

        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Pedido).WithMany(p => p.Items)
               .HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Foto).WithMany()
               .HasForeignKey(x => x.FotoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TamanoPrecio).WithMany()
               .HasForeignKey(x => x.TamanoPrecioId).OnDelete(DeleteBehavior.Restrict);
    }
}
