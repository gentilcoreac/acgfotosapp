using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.IServices;

public interface IFotoAppService
{
    /// <summary>
    /// Upload masivo: guarda los originales en storage privado, persiste las filas en Pendiente y
    /// encola el procesamiento en background. Devuelve las fotos creadas (aún sin derivados).
    /// </summary>
    Task<List<FotoDto>> SubirAsync(SubirFotosInput input);

    /// <summary>Fotos de un curso (o de un álbum puntual) para la galería admin.</summary>
    Task<List<FotoDto>> ListarAsync(long cursoId, long? albumId);

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
}

/// <summary>Descarga del original: los bytes y el nombre con el que lo subió el fotógrafo.</summary>
public record DescargaOriginal(byte[] Contenido, string NombreArchivo);
