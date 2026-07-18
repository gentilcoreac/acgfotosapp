using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.IServices;

/// <summary>
/// Galería de la sesión de familia (ADR-11, Fase 2): a diferencia de <see cref="IFotoAppService"/>
/// (admin, filtra por grupo/participante que llegan por parámetro), acá el alcance sale ÚNICAMENTE
/// de los <c>participanteId</c> firmados en el JWT — el caller nunca puede pedir fotos ajenas.
/// </summary>
public interface IFamiliaGaleriaAppService
{
    /// <summary>Individuales de los participantes de la sesión + grupales de sus grupos. Solo Lista.</summary>
    Task<List<FotoDto>> ListarAsync();

    /// <summary>Bytes del thumb con watermark; null si la foto no existe, no está Lista o no es de la sesión.</summary>
    Task<byte[]?> ObtenerThumbAsync(long fotoId);

    /// <summary>Bytes del preview con watermark; null si la foto no existe, no está Lista o no es de la sesión.</summary>
    Task<byte[]?> ObtenerPreviewAsync(long fotoId);
}
