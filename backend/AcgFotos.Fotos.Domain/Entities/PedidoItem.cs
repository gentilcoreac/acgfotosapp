using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Línea del pedido: una foto en un tamaño, N copias. Congela el precio unitario vigente al
/// confirmar (ADR-07): editar el catálogo después no altera pedidos existentes.
/// </summary>
public class PedidoItem : MultiTenantEntityBase
{
    public long PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public long FotoId { get; set; }
    public Foto Foto { get; set; } = null!;

    public long TamanoPrecioId { get; set; }
    public TamanoPrecio TamanoPrecio { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal PrecioUnitarioSnapshot { get; set; }
}
