using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class FotoMapper
{
    // StorageKey no viaja al DTO (las rutas de storage no se exponen); TenantId tampoco.
    [MapperIgnoreSource(nameof(Foto.TenantId))]
    [MapperIgnoreSource(nameof(Foto.StorageKey))]
    [MapperIgnoreSource(nameof(Foto.Evento))]
    [MapperIgnoreSource(nameof(Foto.Grupo))]
    [MapperIgnoreSource(nameof(Foto.Participante))]
    public partial FotoDto ToDto(Foto entity);
}
