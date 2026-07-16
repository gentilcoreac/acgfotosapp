using AcgFotos.Core.Application;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Foto para el admin (respuesta del upload y galería). No expone <c>StorageKey</c>: las rutas de
/// archivos no viajan en DTOs; los bytes se sirven por endpoints propios (o URL firmada en prod).
/// </summary>
public class FotoDto : DtoBase
{
    public long EventoId { get; set; }
    public long GrupoId { get; set; }

    /// <summary>Null ⇒ foto grupal del grupo.</summary>
    public long? ParticipanteId { get; set; }

    public string NombreArchivoOriginal { get; set; } = string.Empty;
    public int Ancho { get; set; }
    public int Alto { get; set; }
    public long TamanoBytes { get; set; }

    public EstadoProcesamientoFoto EstadoProcesamiento { get; set; }
    public string? ErrorProcesamiento { get; set; }

    public DateTime CreadoEn { get; set; }
}
