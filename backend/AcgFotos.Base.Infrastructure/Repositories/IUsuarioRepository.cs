using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IUsuarioRepository : IEntityBaseRepository<Usuario>
    {
        /// <summary>
        /// Listado paginado de Usuarios proyectando a UsuarioHeaderProjection (sin Includes).
        /// Resuelve TipoLicenciaActivaId y UltimoLogin en SQL via subqueries — evita el N+1
        /// que antes vivía en el AppService (batch dictionaries de licencias + últimos logins).
        /// Si excludeAdmin=true, oculta administradores. Read-only.
        /// </summary>
        Task<PaginationSet<UsuarioHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria, bool excludeAdmin);

        /// <summary>UsuarioAplicaciones del usuario con la Aplicacion incluida. Read-only.</summary>
        Task<IReadOnlyList<UsuarioAplicacion>> GetAplicacionesPermitidasAsync(long usuarioId);

        /// <summary>Usuario por UserName. Read-only.</summary>
        Task<Usuario> GetByUserNameAsync(string userName);

        /// <summary>Ids de roles asignados directamente al usuario.</summary>
        Task<IReadOnlyList<long>> GetRolIdsByUserIdAsync(long usuarioId);

        /// <summary>Ids de roles EFECTIVOS = directos (UsuarioRoles) ∪ roles de sus grupos (UsuarioGrupos → GrupoRoles).</summary>
        Task<IReadOnlyList<long>> GetEffectiveRolIdsByUserIdAsync(long usuarioId);

        /// <summary>
        /// Roles EFECTIVOS del usuario con su origen (directo / grupo) y nombres, para visualización
        /// read-only. Sobre la vista vw_UsuarioRolesEfectivos + nombre del rol y del grupo (left join).
        /// </summary>
        Task<IReadOnlyList<RolEfectivoProjection>> GetRolesEfectivosByUserIdAsync(long usuarioId);

        /// <summary>Historial de un período + tenant, con datos del usuario resueltos por LEFT JOIN. Read-only.</summary>
        Task<IReadOnlyList<UsuarioHistorialReportProjection>> GetUsuarioHistorialByPeriodoAndTenantAsync(string periodo, long tenantId);

        /// <summary>UsuarioHistorial de un período + usuario específico (single, tracked para update).</summary>
        Task<UsuarioHistorial> GetUsuarioHistorialByPeriodoAndUserIdAsync(string periodo, long usuarioId);

        /// <summary>Historial de varios periodos + tenant, con datos del usuario por LEFT JOIN. Single query (evita N+1). Read-only.</summary>
        Task<IReadOnlyList<UsuarioHistorialReportProjection>> GetUsuarioHistorialByPeriodosAndTenantAsync(IReadOnlyList<string> periodos, long tenantId);

        /// <summary>Último login del usuario.</summary>
        Task<UsuarioHistorial> GetUltimoLoginByUserIdAsync(long usuarioId);

        /// <summary>
        /// Usuarios <b>activos</b> (que iniciaron sesión) por mes desde <paramref name="desde"/>.
        /// Cuenta usuarios distintos por mes de FechaLastLogin (UsuarioHistorial). El scope por tenant
        /// lo aplica el filtro global multi-tenant del DbContext. Read-only.
        /// </summary>
        Task<IReadOnlyList<ActividadMensualProjection>> GetUsuariosActivosPorMesAsync(System.DateTime desde);

        /// <summary>
        /// <b>Altas</b> de usuarios (por DateCreated) por mes desde <paramref name="desde"/>. El scope
        /// por tenant lo aplica el filtro global multi-tenant del DbContext. Read-only.
        /// </summary>
        Task<IReadOnlyList<ActividadMensualProjection>> GetAltasUsuariosPorMesAsync(System.DateTime desde);

        /// <summary>Fecha del último login para un set de usuarios. Single query (evita N+1).</summary>
        Task<IReadOnlyDictionary<long, System.DateTime>> GetUltimosLoginsByUserIdsAsync(IReadOnlyList<long> usuarioIds);

        /// <summary>Usuario por UserName con UsuarioAplicaciones. Read-only.</summary>
        Task<Usuario> GetByUserNameWithAplicacionesAsync(string userName);

        /// <summary>Usuario por Id con Roles+Rol y UsuarioAplicaciones+Aplicacion. Read-only.</summary>
        Task<Usuario> GetByIdWithRolesAndAplicacionesAsync(long id);

        /// <summary>Usuario por Id con Roles+Rol. Read-only.</summary>
        Task<Usuario> GetByIdWithRolesAsync(long id);

        /// <summary>Usuario por Id con Roles+Rol y UsuarioAplicaciones+Aplicacion. Tracked (para Update).</summary>
        Task<Usuario> GetByIdWithRolesAndAplicacionesForUpdateAsync(long id);

        /// <summary>Ids de usuarios no-administradores.</summary>
        Task<IReadOnlyList<long>> GetIdsNoAdministradoresAsync();

        /// <summary>
        /// Search paginado con UsuarioAplicaciones incluido. Si excludeAdmin=true, oculta administradores.
        /// Read-only.
        /// </summary>
        Task<PaginationSet<Usuario>> PaginateWithAplicacionesAsync(IListaPaginadaCriteriaBase criteria, bool excludeAdmin);

        /// <summary>Ids de usuarios asignados a un rol dado.</summary>
        Task<IReadOnlyList<long>> GetUsuarioIdsByRolIdAsync(long rolId);

        /// <summary>Usuarios que pertenecen a alguno de los roles indicados, con Roles incluidos. Read-only.</summary>
        Task<IReadOnlyList<Usuario>> GetByRolIdsAsync(IReadOnlyList<long> rolIds);

        /// <summary>Invalida la sesión de los NO-admin de un tenant (al desactivarlo) bumpeando su SecurityStamp
        /// en UNA sola sentencia (NEWID() por fila, atómica). IgnoreQueryFilters: la baja es root-only. Devuelve
        /// la cantidad de filas afectadas.</summary>
        Task<int> BumpSecurityStampForNonAdminsAsync(long tenantId);

        /// <summary>
        /// Administradores de un tenant arbitrario (usuarios con el rol default de nuevo tenant).
        /// <b>Ignora el filtro multi-tenant</b>: lo usa root desde el ABM para ver los admins de OTRO
        /// tenant (Usuario es multi-tenant y el filtro global lo scopea al tenant propio). Read-only.
        /// </summary>
        Task<IReadOnlyList<Usuario>> GetAdministradoresByTenantIdAsync(long tenantId);

        /// <summary>
        /// Usuario por Id <b>ignorando el filtro multi-tenant</b>. Lo usa el flujo de impersonalización
        /// (ADR-0002): root corre en su propio contexto pero necesita resolver al usuario destino (y a sí
        /// mismo al re-impersonar/parar) de OTRO tenant. El caller debe validar el tenant explícitamente.
        /// Read-only.
        /// </summary>
        Task<Usuario> GetByIdIgnoringTenantFilterAsync(long id);

        /// <summary>
        /// <b>Todos</b> los usuarios de un tenant arbitrario <b>ignorando el filtro multi-tenant</b>,
        /// ordenados por UserName. Lo usa el selector de impersonalización (ADR-0002) para que root pueda
        /// elegir cualquier usuario del tenant destino. Read-only.
        /// </summary>
        Task<IReadOnlyList<Usuario>> GetByTenantIdIgnoringFilterAsync(long tenantId);
    }
}
