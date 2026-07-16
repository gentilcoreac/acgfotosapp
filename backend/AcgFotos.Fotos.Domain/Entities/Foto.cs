using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Una foto subida por el fotógrafo. El original vive en el storage privado bajo
/// <c>fotos/originals/{EventoId}/{StorageKey}.jpg</c> y NUNCA se sirve a las familias; al subir se
/// generan los derivados WebP con watermark (<c>fotos/derived/{EventoId}/{StorageKey}_thumb.webp</c>
/// y <c>_preview.webp</c>), que son lo único visible públicamente (ADR-01/ADR-06).
/// </summary>
public class Foto : MultiTenantEntityBase
{
    public long EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public long GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;

    /// <summary>Null ⇒ foto GRUPAL del grupo (visible para todos los participantes del grupo).</summary>
    public long? ParticipanteId { get; set; }
    public Participante? Participante { get; set; }

    /// <summary>
    /// Identificador NO adivinable con el que se arma la key de storage (los Id long son
    /// secuenciales y no deben aparecer en rutas/URLs de archivos).
    /// </summary>
    public Guid StorageKey { get; set; }

    public string NombreArchivoOriginal { get; set; } = null!;
    public int Ancho { get; set; }
    public int Alto { get; set; }
    public long TamanoBytes { get; set; }

    public EstadoProcesamientoFoto EstadoProcesamiento { get; set; } = EstadoProcesamientoFoto.Pendiente;
    public string? ErrorProcesamiento { get; set; }

    public DateTime CreadoEn { get; set; }
}
