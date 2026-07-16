using AcgFotos.Core.Application;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Dtos;

/// <summary>
/// Input de alta/edición. Los tamaños viajan como filas completas y se reconcilian por Id en
/// <c>EventoAppService.SyncCollections</c> (Id 0 = alta; ausente = baja). TenantId no se expone:
/// lo estampa el DbContext.
/// </summary>
public class EventoInputDto : DtoBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? LugarOrganizacion { get; set; }
    public DateTime? Fecha { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public EstadoEvento Estado { get; set; } = EstadoEvento.Borrador;

    public List<TamanoPrecioDto> TamanosPrecios { get; set; } = new();
}
