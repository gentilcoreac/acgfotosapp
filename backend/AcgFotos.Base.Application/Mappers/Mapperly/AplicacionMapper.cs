using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    /// <summary>
    /// Read path de Aplicacion (ABM y selects de aplicación). El write path sigue por el
    /// camino base (SetValues, EF nativo).
    ///
    /// La colección <see cref="AplicacionDto.Permisos"/> NO se mapea desde la entidad: no existe
    /// navigation <c>Aplicacion.Permisos</c> (los permisos se relacionan vía <c>Permiso.AplicacionId</c>).
    /// El AppService la puebla aparte (GetPermisosByIdAsync) y la convierte con <see cref="ToPermisoDtos"/>.
    /// </summary>
    [Mapper]
    public partial class AplicacionMapper
    {
        [MapperIgnoreSource(nameof(Aplicacion.TenantAplicaciones))]
        [MapperIgnoreSource(nameof(Aplicacion.UsuarioAplicaciones))]
        [MapperIgnoreTarget(nameof(AplicacionDto.Permisos))]
        public partial AplicacionDto ToDto(Aplicacion entity);

        /// <summary>
        /// Listado del ABM: proyección SQL → AplicacionDto. Icono/IconoUrl y Permisos no se proyectan
        /// (la grilla no los usa) → quedan default.
        /// </summary>
        [MapperIgnoreTarget(nameof(AplicacionDto.Icono))]
        [MapperIgnoreTarget(nameof(AplicacionDto.IconoUrl))]
        [MapperIgnoreTarget(nameof(AplicacionDto.Permisos))]
        public partial AplicacionDto ToDto(AplicacionHeaderProjection projection);

        /// <summary>
        /// Permisos de una aplicación, cargados sin includes (navegaciones y colecciones vacías).
        /// El ABM sólo consume id/nombre, pero poblamos los escalares por contrato del DTO.
        /// </summary>
        public partial System.Collections.Generic.List<PermisoDto> ToPermisoDtos(
            System.Collections.Generic.IReadOnlyList<Permiso> permisos);

        [MapperIgnoreSource(nameof(Permiso.Endpoints))]
        [MapperIgnoreSource(nameof(Permiso.RolPermisos))]
        [MapperIgnoreSource(nameof(Permiso.PermisosHijos))]
        [MapperIgnoreSource(nameof(Permiso.PermisoPadre))]
        [MapperIgnoreSource(nameof(Permiso.Aplicacion))]
        [MapperIgnoreTarget(nameof(PermisoDto.Endpoints))]
        [MapperIgnoreTarget(nameof(PermisoDto.RolPermisos))]
        [MapperIgnoreTarget(nameof(PermisoDto.PermisoPadre))]
        [MapperIgnoreTarget(nameof(PermisoDto.AplicacionDescripcion))]
        [MapperIgnoreTarget(nameof(PermisoDto.PermisoPadreDescripcion))]
        private partial PermisoDto ToPermisoDto(Permiso permiso);

        /// <summary>
        /// Aplicaciones permitidas del usuario (GetAplicacionesPermitidas). Replica el shape del read
        /// anterior (AutoMapper): escalares + AplicacionNombre aplanado + Aplicacion anidada (la nav
        /// viene incluida en el repo). La nav Usuario no se incluye (queda null) y se ignora para
        /// cortar el ciclo UsuarioAplicacion → Usuario → ...
        /// </summary>
        [MapperIgnoreSource(nameof(UsuarioAplicacion.Usuario))]
        [MapperIgnoreSource(nameof(UsuarioAplicacion.TenantId))]
        [MapperIgnoreTarget(nameof(UsuarioAplicacionDto.Usuario))]
        [MapProperty(
            new[] { nameof(UsuarioAplicacion.Aplicacion), nameof(Aplicacion.Nombre) },
            new[] { nameof(UsuarioAplicacionDto.AplicacionNombre) })]
        public partial UsuarioAplicacionDto ToUsuarioAplicacionDto(UsuarioAplicacion entity);

        public partial System.Collections.Generic.List<UsuarioAplicacionDto> ToUsuarioAplicacionDtos(
            System.Collections.Generic.IReadOnlyList<UsuarioAplicacion> entities);
    }
}
