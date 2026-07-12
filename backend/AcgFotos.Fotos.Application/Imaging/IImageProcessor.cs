namespace AcgFotos.Fotos.Application.Imaging;

/// <summary>
/// Puerto del pipeline de derivados (ADR-01, capa 1): a partir del original genera el preview y el
/// thumbnail, ambos CON marca de agua y en baja resolución. El original nunca se transforma ni se
/// expone; la implementación (ImageSharp) vive en Infrastructure.
/// </summary>
public interface IImageProcessor
{
    /// <exception cref="ImagenInvalidaException">El contenido no es una imagen decodificable.</exception>
    Task<DerivadosFoto> GenerarDerivadosAsync(
        Stream original,
        OpcionesDerivados opciones,
        CancellationToken cancellationToken = default);
}

/// <summary>Parámetros del procesamiento. Los defaults implementan la premisa del producto.</summary>
public record OpcionesDerivados
{
    /// <summary>Texto de la marca de agua, repetido en diagonal sobre toda la imagen.</summary>
    public required string TextoWatermark { get; init; }

    /// <summary>Lado mayor del preview: alcanza para elegir, no para imprimir (ADR-01).</summary>
    public int LadoMayorPreview { get; init; } = 1200;

    /// <summary>Lado mayor del thumbnail de la grilla.</summary>
    public int LadoMayorThumb { get; init; } = 300;

    /// <summary>Calidad JPEG de los derivados (0-100).</summary>
    public int CalidadJpeg { get; init; } = 80;
}

/// <summary>Resultado: bytes JPEG de los derivados + dimensiones del original (para fot_Fotos).</summary>
public record DerivadosFoto
{
    public required byte[] PreviewJpeg { get; init; }
    public required byte[] ThumbJpeg { get; init; }
    public required int AnchoOriginal { get; init; }
    public required int AltoOriginal { get; init; }
}

/// <summary>El contenido subido no es una imagen válida (se traduce a 400 en el borde HTTP).</summary>
public class ImagenInvalidaException : Exception
{
    public ImagenInvalidaException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
