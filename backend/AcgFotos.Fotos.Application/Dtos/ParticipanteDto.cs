using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Fila de participante (participante) del grupo. Id 0 = fila nueva (sync por Id en el update); al crearse el
/// sistema le genera su código de acceso. <see cref="CodigoAcceso"/> es solo de salida (el código
/// activo del participante): lo que venga en el input se ignora.
/// </summary>
public class ParticipanteDto : DtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoAcceso { get; set; }
}
