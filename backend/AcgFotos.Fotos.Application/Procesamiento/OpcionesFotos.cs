namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Config del vertical (sección <c>Fotos</c> del appsettings). Último escalón de la cascada de
/// resolución de marca de agua (ADR-15 §4): resolución/calidad de los derivados cuando el evento y
/// el tenant no tienen opciones de publicación propias. El watermark en sí (qué se dibuja) YA NO se
/// configura acá — sin un <c>PerfilMarcaAgua</c> cargado, se usa la capa por defecto embebida
/// (<c>IFotoStorage.LeerCapaMarcaAguaDefaultAsync</c>, ver <c>FotoProcesadorAppService</c>).
/// </summary>
public class OpcionesFotos
{
    /// <summary>Lado mayor del preview en px. Muy bajo a propósito (premisa ADR-01 + pedido 2026-07-15).</summary>
    public int LadoMayorPreview { get; set; } = 900;

    /// <summary>Lado mayor del thumb de la grilla, en px.</summary>
    public int LadoMayorThumb { get; set; } = 300;

    /// <summary>Calidad WebP (0-100) de ambos derivados. Baja a propósito.</summary>
    public int CalidadDerivados { get; set; } = 55;

    /// <summary>
    /// Molde de la URL que codifican los QR de las tarjetas ({codigo} se reemplaza por el código
    /// del participante). La ruta de canje se implementa en Fase 2; el dominio real se define al deploy.
    /// </summary>
    public string UrlCanjeTemplate { get; set; } = "http://localhost:4200/canje/{codigo}";

    /// <summary>Duración del token de sesión de familia (pedido de Alberto 2026-07-16: 30 minutos).</summary>
    public int DuracionSesionFamiliaMinutos { get; set; } = 30;
}
