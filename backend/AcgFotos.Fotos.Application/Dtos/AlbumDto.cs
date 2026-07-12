using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Fila de álbum (alumno) del curso. Id 0 = fila nueva (sync por Id en el update); al crearse el
/// sistema le genera su código de acceso. <see cref="CodigoAcceso"/> es solo de salida (el código
/// activo del álbum): lo que venga en el input se ignora.
/// </summary>
public class AlbumDto : DtoBase
{
    public string NombreAlumno { get; set; } = string.Empty;
    public string? CodigoAcceso { get; set; }
}
