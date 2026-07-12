using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    /// <summary>
    /// Read path de Usuario (ADR-0001). Reemplaza al AutoMapper en GetByIdAsync/SearchAsync.
    /// El write path (Register / SaveUsuarioEntity / licencias) sigue en AutoMapper porque preserva
    /// campos sensibles de Identity vía ExecuteEdit; acá sólo se mapea de entidad/proyección a DTO.
    /// </summary>
    // RequiredMappingStrategy.Target: sólo exigimos mapear los miembros del DTO destino. La entidad
    // Usuario hereda muchos campos de IdentityUser (PasswordHash, SecurityStamp, normalized, etc.) que
    // no van al DTO; esto evita el ruido de RMG020 sin enumerar cada uno con MapperIgnoreSource.
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class UsuarioMapper
    {
        // TipoLicenciaActiva y UltimoLogin
        // no salen de la entidad: el AppService los setea aparte.
        public UsuarioDto ToDto(Usuario entity)
        {
            var dto = this.MapToDto(entity);
            dto.Bloqueado = entity.LockoutEnabled
                && entity.LockoutEnd.HasValue
                && entity.LockoutEnd.Value > System.DateTimeOffset.UtcNow;
            return dto;
        }

        [MapperIgnoreTarget(nameof(UsuarioDto.Bloqueado))]
        [MapperIgnoreTarget(nameof(UsuarioDto.TipoLicenciaActiva))]
        [MapperIgnoreTarget(nameof(UsuarioDto.UltimoLogin))]
        private partial UsuarioDto MapToDto(Usuario entity);

        // Datos públicos (listado liviano: UserName/Nombre/Apellido/Email). Strategy.Target.
        public partial UsuarioDatosPublicosDto ToDatosPublicosDto(Usuario entity);

        // RolDescripcion <- Rol.Descripcion. Usuario se ignora para cortar el ciclo de referencias.
        [MapProperty(new[] { nameof(UsuarioRol.Rol), nameof(Rol.Descripcion) }, nameof(UsuarioRolDto.RolDescripcion))]
        [MapperIgnoreSource(nameof(UsuarioRol.Usuario))]
        private partial UsuarioRolDto ToDto(UsuarioRol entity);

        // AplicacionNombre <- Aplicacion.Nombre. Usuario (UsuarioDto) se ignora para cortar el ciclo;
        // Aplicacion (AplicacionDto) se mapea acotado para preservar el shape que consume el front.
        [MapProperty(new[] { nameof(UsuarioAplicacion.Aplicacion), nameof(Aplicacion.Nombre) }, nameof(UsuarioAplicacionDto.AplicacionNombre))]
        [MapperIgnoreTarget(nameof(UsuarioAplicacionDto.Usuario))]
        private partial UsuarioAplicacionDto ToDto(UsuarioAplicacion entity);

        // Permisos no se incluye en el include de UsuarioAplicaciones.Aplicacion: se ignora para no
        // arrastrar el grafo de Permiso (que además tiene ciclos PermisoPadre/PermisosHijos).
        [MapperIgnoreTarget(nameof(AplicacionDto.Permisos))]
        private partial AplicacionDto ToDto(Aplicacion entity);

        /// <summary>
        /// Header desde la proyección. TipoLicenciaActiva se arma a mano desde el id (paridad con
        /// AutoMappingProfile: si hay licencia activa, IsActive = true).
        /// </summary>
        public UsuarioHeaderDto ToHeaderDto(UsuarioHeaderProjection projection)
        {
            var dto = this.MapHeader(projection);
            dto.TipoLicenciaActiva = projection.TipoLicenciaActivaId.HasValue
                ? new UsuarioTipoLicenciaDto { TipoLicenciaId = projection.TipoLicenciaActivaId.Value, IsActive = true }
                : null;
            return dto;
        }

        [MapperIgnoreSource(nameof(UsuarioHeaderProjection.TipoLicenciaActivaId))]
        [MapperIgnoreTarget(nameof(UsuarioHeaderDto.TipoLicenciaActiva))]
        private partial UsuarioHeaderDto MapHeader(UsuarioHeaderProjection projection);
    }
}
