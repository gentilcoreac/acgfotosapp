using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Dtos;

public class CursoHeaderDto : HeaderDtoBase
{
    public long EventoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadAlbumes { get; set; }
}
