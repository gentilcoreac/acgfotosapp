namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Builders de cuerpos de request reutilizables (DRY): evitan repetir el shape completo del
    /// UsuarioInputDto en cada test. Para casos de mass-assignment (que agregan campos fuera del DTO,
    /// p. ej. "administrador"/"tenantId") se usa un objeto anónimo inline a propósito — no este builder.
    /// </summary>
    public static class Payloads
    {
        /// <summary>Cuerpo de alta/edición de usuario con defaults; overridear solo lo necesario.</summary>
        public static object User(
            long id = 0,
            string userName = "nuevo",
            string nombre = "N",
            string apellido = "A",
            string? email = null,
            long? telefono = 1122334400,
            bool bloqueado = false,
            object[]? roles = null,
            object[]? apps = null,
            object? licencia = null)
            => new
            {
                id,
                userName,
                nombre,
                apellido,
                email = email ?? $"{userName}@tech-bi.com",
                telefono,
                bloqueado,
                roles = roles ?? System.Array.Empty<object>(),
                usuarioAplicaciones = apps ?? System.Array.Empty<object>(),
                tipoLicenciaActiva = licencia,
            };

        /// <summary>Item de rol para la colección Roles del UsuarioInputDto.</summary>
        public static object Rol(long rolId) => new { rolId };

        /// <summary>Item de aplicación para la colección UsuarioAplicaciones del UsuarioInputDto.</summary>
        public static object App(long aplicacionId, bool @default = false) => new { aplicacionId, @default };

        /// <summary>Asignación de licencia (TipoLicenciaActiva); id=0 = asignación nueva.</summary>
        public static object License(long tipoLicenciaId, long id = 0, bool isActive = true)
            => new { id, tipoLicenciaId, isActive };

        /// <summary>Cuerpo de alta/edición de Rol (RolInputDto). EsDefaultParaNuevoTenant NO va a propósito
        /// (admin-only, endpoint dedicado): los tests de mass-assignment lo agregan inline.</summary>
        public static object RolInput(string descripcion, long id = 0, long[]? permisoIds = null)
            => new
            {
                id,
                descripcion,
                permisoIds = permisoIds ?? System.Array.Empty<long>(),
            };

        /// <summary>Cuerpo de alta/edición de TipoLicencia (TipoLicenciaInputDto). EsDefaultParaNuevoTenant
        /// NO va a propósito (admin-only, endpoint dedicado); los tests de mass-assignment lo agregan inline.</summary>
        public static object TipoLicenciaInput(string descripcion, string? codigo = null, long id = 0, long[]? rolIds = null)
            => new
            {
                id,
                codigoTipoLicencia = codigo ?? descripcion,
                descripcion,
                rolIds = rolIds ?? System.Array.Empty<long>(),
            };

        /// <summary>Cuerpo de alta/edición de Aplicacion (AplicacionDto write path).</summary>
        public static object AplicacionInput(string codigo, string nombre, long id = 0, bool activo = true,
            string? icono = null, string? iconoUrl = null)
            => new { id, codigo, nombre, activo, icono, iconoUrl };

        /// <summary>Cuerpo de alta/edición de Permiso (PermisoInputDto). EsRestringido NO va a propósito
        /// (admin-only, endpoint dedicado); los tests de mass-assignment lo agregan inline.</summary>
        public static object PermisoInput(string nombre, string codigoPermiso, long id = 0,
            string? descripcion = null, bool activo = true, long? aplicacionId = 1, long? permisoPadreId = null,
            object[]? endpoints = null)
            => new
            {
                id,
                nombre,
                codigoPermiso,
                descripcion = descripcion ?? nombre,
                activo,
                aplicacionId,
                permisoPadreId,
                endpoints = endpoints ?? System.Array.Empty<object>(),
            };

        /// <summary>Cuerpo de alta/edición de Endpoint (EndpointDto, write path). SetValues copia TODOS los
        /// escalares (incl. Activo/Route/HttpMethod). Descripcion es read-only computada (sin setter).</summary>
        public static object EndpointInput(string route, string httpMethod = "GET", long id = 0,
            string moduleName = "Custom", string controllerName = "FooController", string actionName = "Bar",
            bool activo = true, string? @namespace = null)
            => new
            {
                id,
                moduleName,
                controllerName,
                actionName,
                httpMethod,
                route,
                activo,
                @namespace = @namespace ?? "AcgFotos.Custom.Controllers.Api",
            };

        /// <summary>Cuerpo de alta/edición de Parametro (ParametroDto, write path del catálogo global).</summary>
        public static object ParametroInput(string nombre, string? valor = "1", long id = 0,
            string? descripcion = null, long aplicacionId = 1, int tipoDato = 3, long? permisoId = null)
            => new
            {
                id,
                nombre,
                valor,
                descripcion = descripcion ?? nombre,
                aplicacionId,
                tipoDato,
                permisoId,
            };

        /// <summary>Cuerpo de alta/edición de Grupo (GrupoInputDto).</summary>
        public static object Grupo(string nombre, long id = 0, long[]? usuarioIds = null, long[]? rolIds = null)
            => new
            {
                id,
                nombre,
                usuarioIds = usuarioIds ?? System.Array.Empty<long>(),
                rolIds = rolIds ?? System.Array.Empty<long>(),
            };

        /// <summary>Cuerpo de alta/edición de Reporte (ReporteInputDto).</summary>
        public static object Reporte(
            string nombre,
            System.Guid workspaceId,
            System.Guid reportId,
            long id = 0,
            long? carpetaId = null)
            => new { id, nombre, workspaceId, reportId, carpetaId };

        /// <summary>Cuerpo de alta/edición de Carpeta (CarpetaInputDto).</summary>
        public static object Carpeta(string nombre, long id = 0)
            => new { id, nombre };

        /// <summary>Cuerpo de alta de RolPBI (RolPBIInputDto).</summary>
        public static object RolPBI(long usuarioId, string nombreRol)
            => new { usuarioId, nombreRol };
    }
}
