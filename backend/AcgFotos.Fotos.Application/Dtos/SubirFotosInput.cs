namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Un archivo del upload masivo, ya leído del multipart (Application no conoce IFormFile).</summary>
public record ArchivoFotoInput(string NombreArchivo, byte[] Contenido);

/// <summary>
/// Input del upload masivo: todos los archivos del request van al mismo destino — un curso
/// (grupales, AlbumId null) o un álbum puntual del curso (individuales del alumno).
/// </summary>
public class SubirFotosInput
{
    public long CursoId { get; set; }
    public long? AlbumId { get; set; }
    public List<ArchivoFotoInput> Archivos { get; set; } = new();
}
