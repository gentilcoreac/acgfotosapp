using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Configurations;

/// <summary>
/// <c>fot_Pedidos</c>. FK al álbum en Restrict: un álbum con pedidos es historial comercial,
/// no se borra en cascada.
/// </summary>
public class PedidoConfig : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("fot_Pedidos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreContacto).HasMaxLength(150).IsRequired();
        builder.Property(x => x.TelefonoContacto).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Total).HasPrecision(10, 2);
        builder.Property(x => x.MercadoPagoPreferenciaId).HasMaxLength(100);

        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Album).WithMany()
               .HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Restrict);
    }
}
