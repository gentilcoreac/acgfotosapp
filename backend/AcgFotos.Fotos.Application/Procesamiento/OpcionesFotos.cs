namespace AcgFotos.Fotos.Application.Procesamiento;

/// <summary>
/// Config del vertical (sección <c>Fotos</c> del appsettings). El texto del watermark queda
/// pendiente de definir con el fotógrafo (notas abiertas); mientras tanto, un default visible.
/// </summary>
public class OpcionesFotos
{
    public string TextoWatermark { get; set; } = "ACG Fotos";
}
