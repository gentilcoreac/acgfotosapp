using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Imaging;

/// <summary>
/// Puerto del pipeline de derivados (ADR-01, capa 1): a partir del original genera el preview y el
/// thumbnail, ambos CON marca de agua y en baja resolución. El original nunca se transforma ni se
/// expone; la implementación (ImageSharp+SkiaSharp, ADR-16) vive en Infrastructure.
/// </summary>
public interface IImageProcessor
{
    /// <exception cref="ImagenInvalidaException">El contenido no es una imagen decodificable.</exception>
    Task<DerivadosFoto> GenerarDerivadosAsync(
        Stream original,
        OpcionesDerivados opciones,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Parámetros del procesamiento. Los defaults de resolución/calidad implementan la premisa del
/// producto (2026-07-15, pedido del fotógrafo): resolución MUY baja — lo capturable no debe servir
/// ni para imprimir ni para compartir con dignidad; alcanza justo para elegir la foto.
/// </summary>
public record OpcionesDerivados
{
    /// <summary>
    /// Capas ya rasterizadas a componer, en orden (ADR-15: el front diseña y rasteriza, la API sólo
    /// compone — nunca dibuja texto). Vacía ⇒ sin marca (la cascada de resolución, no este puerto,
    /// decide qué capas corresponden; ver <c>IConfiguracionFotosResolver</c>).
    /// </summary>
    public IReadOnlyList<CapaComposicion> Capas { get; init; } = [];

    /// <summary>Si las capas también se componen sobre el thumbnail (decisión del perfil, no del processor).</summary>
    public bool MarcarThumb { get; init; } = true;

    /// <summary>Lado mayor del preview: alcanza para elegir, no para imprimir (ADR-01).</summary>
    public int LadoMayorPreview { get; init; } = 900;

    /// <summary>Lado mayor del thumbnail de la grilla.</summary>
    public int LadoMayorThumb { get; init; } = 300;

    /// <summary>Calidad WebP de los derivados (0-100). Baja a propósito.</summary>
    public int Calidad { get; init; } = 55;
}

/// <summary>
/// Una capa ya rasterizada (PNG con transparencia) más su colocación (ADR-15 §2). La API nunca
/// dibuja: coloca, escala (sólo hacia abajo, ADR-15 §8), rota y funde este bitmap con SkiaSharp
/// (ADR-16 — <see cref="ModoFusion"/> mapea a <c>SKBlendMode</c>, no a ImageSharp).
/// </summary>
public record CapaComposicion
{
    /// <summary>Bytes PNG del asset, ya al tamaño máximo de uso (ADR-15 §8) — un solo tile, no la grilla repetida.</summary>
    public required byte[] Asset { get; init; }

    /// <summary>Orden de composición dentro del perfil (capas posteriores se dibujan encima).</summary>
    public int Orden { get; init; }

    public ModoColocacionMarcaAgua ModoColocacion { get; init; }

    /// <summary>Sólo aplica con <see cref="ModoColocacionMarcaAgua.PosicionFija"/>.</summary>
    public PosicionMarcaAgua? Posicion { get; init; }

    /// <summary>Escala del asset en % del ancho de la foto.</summary>
    public float EscalaPorcentaje { get; init; }

    /// <summary>Margen respecto al borde, en % del ancho de la foto. Sólo aplica con posición fija.</summary>
    public float MargenPorcentaje { get; init; }

    /// <summary>
    /// Cada cuánto se repite la marca, en % del ancho de la foto — independiente de
    /// <see cref="EscalaPorcentaje"/>. Sólo aplica en modo repetido.
    /// </summary>
    public float SeparacionPorcentaje { get; init; }

    public float AnguloGrados { get; init; }

    /// <summary>0-1.</summary>
    public float Opacidad { get; init; } = 1f;

    public ModoFusionMarcaAgua ModoFusion { get; init; }
}

/// <summary>Resultado: bytes WebP de los derivados + dimensiones del original (para fot_Fotos).</summary>
public record DerivadosFoto
{
    public required byte[] Preview { get; init; }
    public required byte[] Thumb { get; init; }
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
