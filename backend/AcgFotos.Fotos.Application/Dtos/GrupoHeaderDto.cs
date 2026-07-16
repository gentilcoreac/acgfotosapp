using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

public class GrupoHeaderDto : HeaderDtoBase
{
    public long EventoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadParticipantes { get; set; }
}
