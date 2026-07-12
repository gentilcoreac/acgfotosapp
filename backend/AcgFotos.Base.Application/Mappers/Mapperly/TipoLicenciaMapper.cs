using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    [Mapper]
    public partial class TipoLicenciaMapper
    {
        [MapperIgnoreSource(nameof(TipoLicencia.UsuarioTipoLicencia))]
        [MapperIgnoreSource(nameof(TipoLicencia.TenantLicenses))]
        public partial TipoLicenciaDto ToDto(TipoLicencia entity);

        [MapperIgnoreSource(nameof(TipoLicencia.TipoLicenciaRoles))]
        [MapperIgnoreSource(nameof(TipoLicencia.UsuarioTipoLicencia))]
        [MapperIgnoreSource(nameof(TipoLicencia.TenantLicenses))]
        public partial TipoLicenciaHeaderDto ToHeaderDto(TipoLicencia entity);

        /// <summary>Listado del ABM: proyección SQL → header.</summary>
        public partial TipoLicenciaHeaderDto ToHeaderDto(TipoLicenciaHeaderProjection projection);

        // El join expone RolDescripcion aplanando Rol.Descripcion; el resto de Rol no se mapea.
        [MapperIgnoreSource(nameof(TipoLicenciaRoles.TipoLicenciaId))]
        [MapperIgnoreSource(nameof(TipoLicenciaRoles.TipoLicencia))]
        [MapProperty(
            new[] { nameof(TipoLicenciaRoles.Rol), nameof(Rol.Descripcion) },
            new[] { nameof(TipoLicenciaRolesDto.RolDescripcion) })]
        public partial TipoLicenciaRolesDto ToDto(TipoLicenciaRoles entity);
    }
}
