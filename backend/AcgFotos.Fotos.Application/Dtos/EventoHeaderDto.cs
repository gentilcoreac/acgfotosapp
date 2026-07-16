using AcgFotos.Core.Application;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Dtos;

public class EventoHeaderDto : HeaderDtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? LugarOrganizacion { get; set; }
    public DateTime? Fecha { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public EstadoEvento Estado { get; set; }
}
