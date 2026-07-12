namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Config del vertical (sección <c>Fotos</c> del appsettings). El texto del watermark queda
/// pendiente de definir con el fotógrafo (notas abiertas); mientras tanto, un default visible.
/// </summary>
public class OpcionesFotos
{
    public string TextoWatermark { get; set; } = "ACG Fotos";

    /// <summary>
    /// Molde de la URL que codifican los QR de las tarjetas ({codigo} se reemplaza por el código
    /// del álbum). La ruta de canje se implementa en Fase 2; el dominio real se define al deploy.
    /// </summary>
    public string UrlCanjeTemplate { get; set; } = "http://localhost:4200/canje/{codigo}";
}
