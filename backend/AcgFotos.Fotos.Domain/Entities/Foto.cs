using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Una foto subida por el fotógrafo. El original vive en el storage privado bajo
/// <c>fotos/originals/{EventoId}/{StorageKey}.jpg</c> y NUNCA se sirve a las familias; al subir se
/// generan los derivados con watermark (<c>fotos/derived/{EventoId}/{StorageKey}_thumb.jpg</c> y
/// <c>_preview.jpg</c>), que son lo único visible públicamente (ADR-01/ADR-06).
/// </summary>
public class Foto : MultiTenantEntityBase
{
    public long EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public long CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    /// <summary>Null ⇒ foto GRUPAL del curso (visible para todos los álbumes del curso).</summary>
    public long? AlbumId { get; set; }
    public Album? Album { get; set; }

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
