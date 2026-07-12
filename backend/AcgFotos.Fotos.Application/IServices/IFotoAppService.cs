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
}
