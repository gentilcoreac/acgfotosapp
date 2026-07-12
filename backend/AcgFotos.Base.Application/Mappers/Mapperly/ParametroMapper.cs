using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    [Mapper]
    public partial class ParametroMapper
    {
        // AplicacionNombre se aplana desde Aplicacion.Nombre; el resto de Aplicacion y la nav
        // Permiso no se mapean (el DTO sólo expone AplicacionId/PermisoId).
        [MapperIgnoreSource(nameof(Parametro.Permiso))]
        [MapProperty(
            new[] { nameof(Parametro.Aplicacion), nameof(Aplicacion.Nombre) },
            new[] { nameof(ParametroDto.AplicacionNombre) })]
        public partial ParametroDto ToDto(Parametro entity);

        // Parametro → ParametroValorOutput (edición de parámetros por tenant). Sólo se copian los
        // escalares propios del parámetro; los overrides por tenant/usuario/rol y el valor default
        // los completa el AppService (ParametrosPorTenantAplicacionAsync).
        [MapperIgnoreSource(nameof(Parametro.Aplicacion))]
        [MapperIgnoreSource(nameof(Parametro.Permiso))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.EsPrivado))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.TagCategoria))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorUsuarioId))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorRolId))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorTenantId))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorId))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorTenant))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorUsuario))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorRol))]
        [MapperIgnoreTarget(nameof(ParametroValorOutput.ParametroValorDefaultValue))]
        public partial ParametroValorOutput ToParametroValorOutput(Parametro entity);
    }
}
