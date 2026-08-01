namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Mapea 1 a 1 a <c>GraphicsOptions.ColorBlendingMode</c> (ImageSharp) y a
/// <c>globalCompositeOperation</c> (canvas) — misma especificación W3C en ambos lados (ADR-15 §3).
/// </summary>
public enum ModoFusionMarcaAgua
{
    Normal = 0,

    /// <summary>Resuelve que el blanco al 50% desaparezca sobre un vestido claro.</summary>
    Superponer = 1,

    Diferencia = 2,
}
