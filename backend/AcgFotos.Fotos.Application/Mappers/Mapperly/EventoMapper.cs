using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class EventoMapper
{
    // Detalle (getById / respuesta del update): incluye el catálogo. TenantId no viaja al DTO.
    // PerfilMarcaAgua/OpcionesPublicacion (navegación) se ignoran: el DTO sólo expone el Id (mapea
    // por convención, mismo nombre) — el combo del ABM no necesita el perfil/opciones completos.
    [MapperIgnoreSource(nameof(Evento.TenantId))]
    [MapperIgnoreSource(nameof(Evento.Grupos))]
    [MapperIgnoreSource(nameof(Evento.PerfilMarcaAgua))]
    [MapperIgnoreSource(nameof(Evento.OpcionesPublicacion))]
    public partial EventoDto ToDto(Evento entity);

    // Listado: solo escalares.
    [MapperIgnoreSource(nameof(Evento.TenantId))]
    [MapperIgnoreSource(nameof(Evento.Grupos))]
    [MapperIgnoreSource(nameof(Evento.TamanosPrecios))]
    [MapperIgnoreSource(nameof(Evento.PerfilMarcaAgua))]
    [MapperIgnoreSource(nameof(Evento.OpcionesPublicacion))]
    public partial EventoHeaderDto ToHeaderDto(Evento entity);

    [MapperIgnoreSource(nameof(TamanoPrecio.TenantId))]
    [MapperIgnoreSource(nameof(TamanoPrecio.EventoId))]
    [MapperIgnoreSource(nameof(TamanoPrecio.Evento))]
    public partial TamanoPrecioDto ToDto(TamanoPrecio entity);
}
