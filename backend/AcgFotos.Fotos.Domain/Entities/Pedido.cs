using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Pedido de impresiones de una familia. Confirmado es INMUTABLE para la familia (ADR-07): para
/// cambiarlo, el fotógrafo lo cancela y la familia rehace. Los campos de pago existen desde el MVP
/// aunque el pago online llegue en Fase 3 (ADR-08).
/// </summary>
public class Pedido : MultiTenantEntityBase
{
    public long ParticipanteId { get; set; }
    public Participante Participante { get; set; } = null!;

    public string NombreContacto { get; set; } = null!;
    public string TelefonoContacto { get; set; } = null!;

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    /// <summary>Snapshot del total al confirmar (suma de los items con su precio congelado).</summary>
    public decimal Total { get; set; }

    public DateTime CreadoEn { get; set; }

    // --- Pago (se usan recién en Fase 3; acá para no migrar dolorosamente, ADR-08) ---
    public MedioPago? MedioPago { get; set; }
    public string? MercadoPagoPreferenciaId { get; set; }
    public DateTime? PagadoEn { get; set; }

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
}
