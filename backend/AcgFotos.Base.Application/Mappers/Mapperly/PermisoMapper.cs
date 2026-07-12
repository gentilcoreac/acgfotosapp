using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    /// <summary>
    /// Read path de Permiso. El detalle (PermisoDto) incluye campos del Endpoint concatenados
    /// (EndpointHttpMethodRoute, Descripcion) — el caso "Plan B" del ADR-0001 — que se resuelven
    /// con el método hand-written <see cref="ToEndpointDto"/> que Mapperly usa para la colección.
    /// El write path sigue en AutoMapper (preserva EsRestringido/CodigoPermiso vía .Ignore()).
    /// </summary>
    [Mapper]
    public partial class PermisoMapper
    {
        // Detalle (GetById + output del Update). RolPermisos/PermisosHijos no se exponen en el
        // detalle del ABM (no se cargan en GetByIdWithDetails); quedan en la lista vacía del ctor.
        [MapperIgnoreSource(nameof(Permiso.RolPermisos))]
        [MapperIgnoreSource(nameof(Permiso.PermisosHijos))]
        [MapperIgnoreTarget(nameof(PermisoDto.RolPermisos))]
        [MapProperty(
            new[] { nameof(Permiso.Aplicacion), nameof(Aplicacion.Nombre) },
            new[] { nameof(PermisoDto.AplicacionDescripcion) })]
        [MapProperty(
            new[] { nameof(Permiso.PermisoPadre), nameof(Permiso.Nombre) },
            new[] { nameof(PermisoDto.PermisoPadreDescripcion) })]
        public partial PermisoDto ToDto(Permiso entity);

        // Header desde la proyección del listado (Search). Proyección ya plana.
        public partial PermisoHeaderDto ToHeaderDto(PermisoHeaderProjection projection);

        // Header desde la entidad (GetAll). Aplicacion/PermisoPadre pueden venir null (sin includes);
        // Mapperly genera acceso null-safe en el aplanado.
        [MapperIgnoreSource(nameof(Permiso.Endpoints))]
        [MapperIgnoreSource(nameof(Permiso.RolPermisos))]
        [MapperIgnoreSource(nameof(Permiso.PermisosHijos))]
        [MapperIgnoreSource(nameof(Permiso.PermisoPadreId))]
        [MapperIgnoreSource(nameof(Permiso.AplicacionId))]
        [MapperIgnoreSource(nameof(Permiso.EsRestringido))]
        [MapProperty(
            new[] { nameof(Permiso.Aplicacion), nameof(Aplicacion.Nombre) },
            new[] { nameof(PermisoHeaderDto.AplicacionDescripcion) })]
        [MapProperty(
            new[] { nameof(Permiso.PermisoPadre), nameof(Permiso.Nombre) },
            new[] { nameof(PermisoHeaderDto.PermisoPadreDescripcion) })]
        public partial PermisoHeaderDto ToHeaderDtoFromEntity(Permiso entity);

        // Write (edición): aplica los escalares editables del input sobre la entidad tracked.
        // EsRestringido (admin-only, endpoint dedicado) y las navegaciones/colecciones se preservan;
        // Endpoints se reconcilia aparte (UpdateEndpoints: clear + add explícito).
        [MapperIgnoreSource(nameof(PermisoInputDto.Endpoints))]
        [MapperIgnoreTarget(nameof(Permiso.Endpoints))]
        [MapperIgnoreTarget(nameof(Permiso.PermisoPadre))]
        [MapperIgnoreTarget(nameof(Permiso.Aplicacion))]
        [MapperIgnoreTarget(nameof(Permiso.RolPermisos))]
        [MapperIgnoreTarget(nameof(Permiso.PermisosHijos))]
        [MapperIgnoreTarget(nameof(Permiso.EsRestringido))]
        public partial void ApplyInput(PermisoInputDto src, Permiso dest);

        // Hand-written: aplana los campos del Endpoint y arma las dos cadenas concatenadas.
        // Mapperly lo usa automáticamente al mapear la colección Endpoints del PermisoDto.
        private static PermisoEndpointDto ToEndpointDto(PermisoEndpoint pe) => new PermisoEndpointDto
        {
            Id = pe.Id,
            PermisoId = pe.PermisoId,
            EndpointId = pe.EndpointId,
            EndpointModuleName = pe.Endpoint == null ? "" : pe.Endpoint.ModuleName,
            EndpointControllerName = pe.Endpoint == null ? "" : pe.Endpoint.ControllerName,
            EndpointRoute = pe.Endpoint == null ? "" : pe.Endpoint.Route,
            EndpointHttpMethodRoute = pe.Endpoint == null
                ? ""
                : pe.Endpoint.HttpMethod + " - " + pe.Endpoint.Route,
            Descripcion = pe.Endpoint == null
                ? ""
                : pe.Endpoint.ModuleName + " - " + pe.Endpoint.ControllerName + " - " +
                  pe.Endpoint.ActionName + " (" + pe.Endpoint.Route + ")",
        };
    }
}
