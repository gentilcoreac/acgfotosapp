using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.IServices;

public interface IFotoAppService
{
    /// <summary>
    /// Upload masivo: guarda los originales en storage privado, persiste las filas en Pendiente y
    /// encola el procesamiento en background. Devuelve las fotos creadas (aún sin derivados).
    /// </summary>
    Task<List<FotoDto>> SubirAsync(SubirFotosInput input);

    /// <summary>Fotos de un grupo (o de un participante puntual) para la galería admin.</summary>
    Task<List<FotoDto>> ListarAsync(long grupoId, long? participanteId);

    /// <summary>Bytes del thumb con watermark; null si la foto no existe o aún no está Lista.</summary>
    Task<byte[]?> ObtenerThumbAsync(long fotoId);

    /// <summary>Bytes del preview con watermark; null si la foto no existe o aún no está Lista.</summary>
    Task<byte[]?> ObtenerPreviewAsync(long fotoId);

    /// <summary>
    /// El ORIGINAL limpio para imprimir (ADR-06: solo existe en endpoints admin autenticados).
    /// Null si la foto no existe en el tenant.
    /// </summary>
    Task<DescargaOriginal?> ObtenerOriginalAsync(long fotoId);

    /// <summary>Borra la foto: fila + original + derivados. False si no existe en el tenant.</summary>
    Task<bool> EliminarAsync(long fotoId);

    /// <summary>
    /// Cuántas fotos se regenerarían de este evento (spec `regeneracion-derivados`: el conteo se
    /// informa ANTES de encolar, para el diálogo de confirmación). Excepción si el evento no existe
    /// en el tenant actual.
    /// </summary>
    Task<int> ContarParaRegenerarAsync(long eventoId);

    /// <summary>
    /// Marca esas fotos Pendiente y las encola en el worker existente (D9: sin infraestructura
    /// nueva). Devuelve cuántas se encolaron. El original nunca se toca.
    /// </summary>
    Task<int> RegenerarAsync(long eventoId);
}

/// <summary>Descarga del original: los bytes y el nombre con el que lo subió el fotógrafo.</summary>
public record DescargaOriginal(byte[] Contenido, string NombreArchivo);
