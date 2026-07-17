namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Config del vertical (sección <c>Fotos</c> del appsettings). El texto del watermark queda
/// pendiente de definir con el fotógrafo (notas abiertas); mientras tanto, un default visible.
/// </summary>
public class OpcionesFotos
{
    public string TextoWatermark { get; set; } = "ACG Fotos";

    /// <summary>Lado mayor del preview en px. Muy bajo a propósito (premisa ADR-01 + pedido 2026-07-15).</summary>
    public int LadoMayorPreview { get; set; } = 900;

    /// <summary>Lado mayor del thumb de la grilla, en px.</summary>
    public int LadoMayorThumb { get; set; } = 300;

    /// <summary>Calidad WebP (0-100) de ambos derivados. Baja a propósito.</summary>
    public int CalidadDerivados { get; set; } = 55;

    /// <summary>Opacidad del texto del watermark (0-1). Pedido del negocio: mitad transparente.</summary>
    public float OpacidadWatermark { get; set; } = 0.5f;

    /// <summary>
    /// Molde de la URL que codifican los QR de las tarjetas ({codigo} se reemplaza por el código
    /// del participante). La ruta de canje se implementa en Fase 2; el dominio real se define al deploy.
    /// </summary>
    public string UrlCanjeTemplate { get; set; } = "http://localhost:4200/canje/{codigo}";

    /// <summary>Duración del token de sesión de familia (pedido de Alberto 2026-07-16: 30 minutos).</summary>
    public int DuracionSesionFamiliaMinutos { get; set; } = 30;
}
