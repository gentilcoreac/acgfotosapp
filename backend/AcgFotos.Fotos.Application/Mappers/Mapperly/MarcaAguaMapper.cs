using Riok.Mapperly.Abstractions;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Mappers.Mapperly;

[Mapper]
public partial class PerfilMarcaAguaMapper
{
    [MapperIgnoreSource(nameof(PerfilMarcaAgua.TenantId))]
    [MapperIgnoreTarget(nameof(PerfilMarcaAguaDto.Avisos))] // lo completa PerfilMarcaAguaAppService.ToOutput
    public partial PerfilMarcaAguaDto ToDto(PerfilMarcaAgua entity);

    [MapperIgnoreSource(nameof(CapaMarcaAgua.TenantId))]
    [MapperIgnoreSource(nameof(CapaMarcaAgua.PerfilMarcaAguaId))]
    [MapperIgnoreSource(nameof(CapaMarcaAgua.PerfilMarcaAgua))]
    public partial CapaMarcaAguaDto ToDto(CapaMarcaAgua entity);
}

[Mapper]
public partial class OpcionesPublicacionMapper
{
    [MapperIgnoreSource(nameof(OpcionesPublicacion.TenantId))]
    public partial OpcionesPublicacionDto ToDto(OpcionesPublicacion entity);
}
