using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    [Mapper]
    public partial class RolMapper
    {
        [MapperIgnoreSource(nameof(Rol.TipoLicenciaRoles))]
        public partial RolDto ToDto(Rol entity);

        [MapperIgnoreSource(nameof(Rol.RolPermisos))]
        [MapperIgnoreSource(nameof(Rol.TipoLicenciaRoles))]
        public partial RolHeaderDto ToHeaderDto(Rol entity);

        public partial RolHeaderDto ToHeaderDto(RolHeaderProjection projection);

        [MapperIgnoreSource(nameof(RolPermiso.Rol))]
        [MapperIgnoreSource(nameof(RolPermiso.Permiso))]
        [MapperIgnoreTarget(nameof(RolPermisoDto.Rol))]
        [MapperIgnoreTarget(nameof(RolPermisoDto.Permiso))]
        public partial RolPermisoDto ToDto(RolPermiso entity);
    }
}
