using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Storage;

/// <summary>
/// Única fuente de las keys de storage de las fotos (ADR-06): la separación
/// <c>originals/</c> vs <c>derived/</c> es estructural — el código público solo puede armar keys
/// de derivados, y la descarga de originales existe únicamente en endpoints admin.
/// </summary>
public static class FotoStorageKeys
{
    public static string Original(Foto foto) =>
        $"fotos/originals/{foto.EventoId}/{foto.StorageKey:N}.jpg";

    public static string Preview(Foto foto) =>
        $"fotos/derived/{foto.EventoId}/{foto.StorageKey:N}_preview.webp";

    public static string Thumb(Foto foto) =>
        $"fotos/derived/{foto.EventoId}/{foto.StorageKey:N}_thumb.webp";

    /// <summary>Asset PNG de una capa de marca de agua (ADR-15 §7: sin pérdida, nunca WebP).</summary>
    public static string CapaMarcaAgua(long perfilMarcaAguaId, Guid storageKey) =>
        $"fotos/watermarks/{perfilMarcaAguaId}/{storageKey:N}.png";
}
