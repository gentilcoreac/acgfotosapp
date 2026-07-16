namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>Un archivo del upload masivo, ya leído del multipart (Application no conoce IFormFile).</summary>
public record ArchivoFotoInput(string NombreArchivo, byte[] Contenido);

/// <summary>
/// Input del upload masivo: todos los archivos del request van al mismo destino — un grupo
/// (grupales, ParticipanteId null) o un participante puntual del grupo (individuales del participante).
/// </summary>
public class SubirFotosInput
{
    public long GrupoId { get; set; }
    public long? ParticipanteId { get; set; }
    public List<ArchivoFotoInput> Archivos { get; set; } = new();
}
