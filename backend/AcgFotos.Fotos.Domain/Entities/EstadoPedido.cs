namespace AcgFotos.Fotos.Domain.Entities;

public enum EstadoPedido
{
    /// <summary>Confirmado por la familia; pendiente de impresión.</summary>
    Pendiente = 0,

    /// <summary>Pagado online (Fase 3). Con pago en efectivo el pedido salta directo a Impreso.</summary>
    Pagado = 1,

    /// <summary>Copias impresas, listas para entregar.</summary>
    Impreso = 2,

    /// <summary>Entregado a la familia (y cobrado, si era efectivo).</summary>
    Entregado = 3,

    Cancelado = 4,
}
