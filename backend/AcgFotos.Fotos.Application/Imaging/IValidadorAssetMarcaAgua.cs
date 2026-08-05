namespace AcgFotos.Fotos.Application.Imaging;

/// <summary>
/// Valida el asset de una capa al subirlo (ADR-15 §7, design D5): formato real por decodificación
/// (no extensión/content-type), dimensiones vía identificación de cabecera ANTES de decodificar
/// completo (ataja bombas de descompresión), techo de peso. Sólo se acepta PNG: es el único formato
/// con transparencia real y ADR-15 exige guardar sin pérdida — no tiene sentido re-codificar otra
/// cosa a PNG después.
/// </summary>
public interface IValidadorAssetMarcaAgua
{
    /// <exception cref="AcgFotos.Core.Exceptions.BusinessValidationException">
    /// No es una imagen decodificable, no es PNG, o excede el techo de dimensiones/peso configurado.
    /// El mensaje incluye el número concreto (D12): no hay "archivo inválido" genérico.
    /// </exception>
    Task<ResultadoValidacionAsset> ValidarAsync(byte[] contenido, CancellationToken cancellationToken = default);

    /// <summary>
    /// ¿El asset alcanza en resolución para la escala elegida? (ADR-15 §8: agrandar un bitmap no
    /// tiene arreglo, así que hay que saberlo ANTES, no cuando ya se ve borroso). Contra una foto de
    /// referencia del lado mayor máximo razonable (default 1600px, el tope que cita ADR-15 §8) —
    /// no depende del evento: un perfil default del tenant puede terminar en cualquier evento, cada
    /// uno con su propia resolución de publicación.
    /// </summary>
    EvaluacionEscala EvaluarEscala(int anchoAssetPx, float escalaPorcentaje, int anchoReferenciaFotoPx = 1600);
}

public record EvaluacionEscala
{
    public required int AnchoNecesarioPx { get; init; }
    public required bool Alcanza { get; init; }
}

/// <summary>Hechos detectados del asset ya validado. La fraseo del aviso al fotógrafo es responsabilidad del caller (AppService).</summary>
public record ResultadoValidacionAsset
{
    public required int AnchoPx { get; init; }
    public required int AltoPx { get; init; }

    /// <summary>False ⇒ el PNG no tiene canal alfa real (Rgb/Grayscale/Palette sin tRNS): se va a componer con su fondo sólido.</summary>
    public required bool TieneCanalAlfa { get; init; }
}
