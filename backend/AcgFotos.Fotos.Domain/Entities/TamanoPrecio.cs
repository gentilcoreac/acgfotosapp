using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Catálogo de tamaños de impresión y precios de UN evento (los precios cambian entre eventos).
/// Los pedidos NO referencian este precio como vigente: congelan una copia en
/// <see cref="PedidoItem.PrecioUnitarioSnapshot"/> (ADR-07).
/// </summary>
public class TamanoPrecio : MultiTenantEntityBase
{
    public long EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    /// <summary>Ej. "10x15", "13x18", "20x30".</summary>
    public string Nombre { get; set; } = null!;

    public decimal PrecioUnitario { get; set; }

    /// <summary>Orden de presentación en la UI.</summary>
    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}
