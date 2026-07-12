namespace AcgFotos.Fotos.Domain.Entities;

public enum EstadoProcesamientoFoto
{
    /// <summary>Original guardado; los derivados (thumb/preview con watermark) aún no se generaron.</summary>
    Pendiente = 0,

    /// <summary>Derivados generados: la foto ya puede mostrarse a las familias.</summary>
    Lista = 1,

    /// <summary>Falló la generación de derivados (detalle en <see cref="Foto.ErrorProcesamiento"/>).</summary>
    Error = 2,
}
