using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class EventoMapper
{
    // Detalle (getById / respuesta del update): incluye el catálogo. TenantId no viaja al DTO.
    [MapperIgnoreSource(nameof(Evento.TenantId))]
    [MapperIgnoreSource(nameof(Evento.Cursos))]
    public partial EventoDto ToDto(Evento entity);

    // Listado: solo escalares.
    [MapperIgnoreSource(nameof(Evento.TenantId))]
    [MapperIgnoreSource(nameof(Evento.Cursos))]
    [MapperIgnoreSource(nameof(Evento.TamanosPrecios))]
    public partial EventoHeaderDto ToHeaderDto(Evento entity);

    [MapperIgnoreSource(nameof(TamanoPrecio.TenantId))]
    [MapperIgnoreSource(nameof(TamanoPrecio.EventoId))]
    [MapperIgnoreSource(nameof(TamanoPrecio.Evento))]
    public partial TamanoPrecioDto ToDto(TamanoPrecio entity);
}
