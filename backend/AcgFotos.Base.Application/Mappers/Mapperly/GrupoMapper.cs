using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    [Mapper]
    public partial class GrupoMapper
    {
        // Detalle (getById): incluye los miembros. TenantId no viaja al DTO.
        [MapperIgnoreSource(nameof(Grupo.TenantId))]
        public partial GrupoDto ToDto(Grupo entity);

        // Listado: la proyección ya trae CantidadMiembros calculado en SQL.
        public partial GrupoHeaderDto ToHeaderDto(GrupoHeaderProjection projection);

        // Ids-only y null-safe: NO aplana Usuario (en el path de update las filas nuevas tienen
        // Usuario==null). Los campos de display (UsuarioNombre/..., UsuarioTipoLicenciaActivaId) los llena
        // GrupoAppService.GetByIdAsync cuando el Usuario está cargado (detalle). Por eso van como IgnoreTarget acá.
        [MapperIgnoreSource(nameof(UsuarioGrupo.Usuario))]
        [MapperIgnoreSource(nameof(UsuarioGrupo.Grupo))]
        [MapperIgnoreSource(nameof(UsuarioGrupo.TenantId))]
        [MapperIgnoreTarget(nameof(UsuarioGrupoDto.UsuarioUserName))]
        [MapperIgnoreTarget(nameof(UsuarioGrupoDto.UsuarioNombre))]
        [MapperIgnoreTarget(nameof(UsuarioGrupoDto.UsuarioApellido))]
        [MapperIgnoreTarget(nameof(UsuarioGrupoDto.UsuarioTipoLicenciaActivaId))]
        public partial UsuarioGrupoDto ToDto(UsuarioGrupo entity);

        // Ids-only: el detalle del grupo expone sus roles como ids (el front marca los checkboxes).
        [MapperIgnoreSource(nameof(GrupoRol.Grupo))]
        [MapperIgnoreSource(nameof(GrupoRol.Rol))]
        [MapperIgnoreSource(nameof(GrupoRol.TenantId))]
        public partial GrupoRolDto ToDto(GrupoRol entity);
    }
}
