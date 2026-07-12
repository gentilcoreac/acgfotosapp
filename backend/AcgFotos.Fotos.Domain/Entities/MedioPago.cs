namespace AcgFotos.Fotos.Domain.Entities;

public enum MedioPago
{
    /// <summary>Se cobra al entregar (el modo actual del negocio).</summary>
    Efectivo = 0,

    /// <summary>Checkout Pro (Fase 3).</summary>
    MercadoPago = 1,
}
