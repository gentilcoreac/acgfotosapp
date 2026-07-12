using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class UsuarioRepository : EntityBaseRepository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<PaginationSet<UsuarioHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria, bool excludeAdmin)
        {
            IQueryable<Usuario> query = this.DbContext.Set<Usuario>().AsNoTracking();
            if (excludeAdmin)
            {
                query = query.Where(u => !u.Administrador);
            }

            // Búsqueda libre server-side (nombre/apellido/usuario/email). Habilita el typeahead del
            // selector de miembros de grupos y el buscador del propio listado de usuarios.
            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(u =>
                    u.Nombre.Contains(s) ||
                    u.Apellido.Contains(s) ||
                    u.UserName.Contains(s) ||
                    u.Email.Contains(s));
            }

            // Bloqueado = estado real de lockout (ADR-0006), equivalente a UserManager.IsLockedOut:
            // política habilitada (LockoutEnabled) Y LockoutEnd en el futuro.
            var now = System.DateTimeOffset.UtcNow;
            var projection = query.Select(u => new UsuarioHeaderProjection
            {
                Id = u.Id,
                UserName = u.UserName,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Bloqueado = u.LockoutEnabled && u.LockoutEnd != null && u.LockoutEnd > now,
                UltimoLogin = this.DbContext.Set<UsuarioHistorial>()
                    .Where(h => h.UsuarioId == u.Id)
                    .Max(h => (System.DateTime?)h.FechaLastLogin),
                TipoLicenciaActivaId = this.DbContext.Set<UsuarioTipoLicencia>()
                    .Where(l => l.UsuarioId == u.Id && l.IsActive)
                    .Select(l => (long?)l.TipoLicenciaId)
                    .FirstOrDefault()
            });

            return this.BuildPaginationAsync(projection, criteria);
        }

        // Activos por mes: trae los pares distintos (usuario, año, mes) en SQL (SELECT DISTINCT) y
        // agrupa/cuenta en memoria. La distinción evita contar dos veces a un usuario con más de un
        // registro en el mismo mes; el volumen es chico (activos del tenant en la ventana pedida).
        // El scope por tenant lo aplica el filtro global multi-tenant (UsuarioHistorial es multi-tenant).
        public async Task<IReadOnlyList<ActividadMensualProjection>> GetUsuariosActivosPorMesAsync(System.DateTime desde)
        {
            var distintos = await this.DbContext.Set<UsuarioHistorial>().AsNoTracking()
                .Where(h => h.FechaLastLogin >= desde)
                .Select(h => new { h.UsuarioId, h.FechaLastLogin.Year, h.FechaLastLogin.Month })
                .Distinct()
                .ToListAsync();

            return distintos
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new ActividadMensualProjection
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Cantidad = g.Count()
                })
                .ToList();
        }

        // Altas por mes: cuenta usuarios por mes de DateCreated. El scope por tenant lo aplica el
        // filtro global multi-tenant del DbContext (Usuario es multi-tenant), igual que PaginateHeadersAsync.
        public async Task<IReadOnlyList<ActividadMensualProjection>> GetAltasUsuariosPorMesAsync(System.DateTime desde) =>
            await this.DbContext.Set<Usuario>().AsNoTracking()
                .Where(u => u.DateCreated >= desde)
                .GroupBy(u => new { u.DateCreated.Year, u.DateCreated.Month })
                .Select(g => new ActividadMensualProjection
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Cantidad = g.Count()
                })
                .ToListAsync();

        public async Task<IReadOnlyList<UsuarioAplicacion>> GetAplicacionesPermitidasAsync(long usuarioId) =>
            await this.DbContext.Set<UsuarioAplicacion>()
                .AsNoTracking()
                .Include(x => x.Aplicacion)
                .Where(x => x.UsuarioId == usuarioId)
                .ToListAsync();

        public Task<Usuario> GetByUserNameAsync(string userName) =>
            this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserName == userName);

        public async Task<IReadOnlyList<long>> GetRolIdsByUserIdAsync(long usuarioId) =>
            await this.DbContext.Set<UsuarioRol>()
                .AsNoTracking()
                .Where(x => x.UsuarioId == usuarioId)
                .Select(x => x.RolId)
                .ToListAsync();

        // Roles efectivos = directos (UsuarioRoles) ∪ de sus grupos (UsuarioGrupos → GrupoRoles),
        // ACOTADOS por la licencia del usuario (la licencia es tope duro). Fuente única de verdad: la
        // vista vw_UsuarioRolesEfectivos. Acá sólo interesa el set de RolId permitidos por licencia
        // (filtro `PermitidoPorLicencia`), deduplicado (la vista trae una fila por origen). Lo usa el
        // menú, que ya es un path de no-root (root arma su menú por PermisoRoot, no pasa por acá).
        public async Task<IReadOnlyList<long>> GetEffectiveRolIdsByUserIdAsync(long usuarioId) =>
            await this.DbContext.Set<UsuarioRolEfectivo>()
                .Where(x => x.UsuarioId == usuarioId && x.PermitidoPorLicencia)
                .Select(x => x.RolId)
                .Distinct()
                .ToListAsync();

        // Roles efectivos con su origen + nombres (read-only): la misma vista, joineada con Rol
        // (descripción) y, por left join, con Grupo (nombre) cuando el origen es un grupo.
        public async Task<IReadOnlyList<RolEfectivoProjection>> GetRolesEfectivosByUserIdAsync(long usuarioId) =>
            await (from v in this.DbContext.Set<UsuarioRolEfectivo>()
                   where v.UsuarioId == usuarioId
                   join r in this.DbContext.Set<Rol>() on v.RolId equals r.Id
                   join g in this.DbContext.Set<Grupo>() on v.GrupoId equals g.Id into grupos
                   from g in grupos.DefaultIfEmpty()
                   orderby r.Descripcion, v.Origen
                   select new RolEfectivoProjection
                   {
                       RolId = v.RolId,
                       RolDescripcion = r.Descripcion,
                       Origen = v.Origen,
                       GrupoId = v.GrupoId,
                       GrupoNombre = g != null ? g.Nombre : null,
                       PermitidoPorLicencia = v.PermitidoPorLicencia
                   }).ToListAsync();

        // LEFT JOIN explícito a Usuario (no hay FK/navegación): si el usuario fue borrado,
        // los campos de display quedan null pero la fila igual cuenta. Filtra por TenantId propio.
        public async Task<IReadOnlyList<UsuarioHistorialReportProjection>> GetUsuarioHistorialByPeriodoAndTenantAsync(string periodo, long tenantId) =>
            await (from h in this.DbContext.Set<UsuarioHistorial>().AsNoTracking()
                   where h.Periodo == periodo && h.TenantId == tenantId
                   join u in this.DbContext.Set<Usuario>().AsNoTracking() on h.UsuarioId equals u.Id into uj
                   from u in uj.DefaultIfEmpty()
                   select new UsuarioHistorialReportProjection
                   {
                       Id = h.Id,
                       UsuarioId = h.UsuarioId,
                       TenantId = h.TenantId,
                       Periodo = h.Periodo,
                       FechaLastLogin = h.FechaLastLogin,
                       Nombre = u != null ? u.Nombre : null,
                       Apellido = u != null ? u.Apellido : null,
                       UserName = u != null ? u.UserName : null,
                       Email = u != null ? u.Email : null
                   }).ToListAsync();

        // Sin AsNoTracking: el caller (UsuarioHistorialAppService) lo actualiza después.
        public Task<UsuarioHistorial> GetUsuarioHistorialByPeriodoAndUserIdAsync(string periodo, long usuarioId) =>
            this.DbContext.Set<UsuarioHistorial>()
                .FirstOrDefaultAsync(x => x.Periodo == periodo && x.UsuarioId == usuarioId);

        public async Task<IReadOnlyList<UsuarioHistorialReportProjection>> GetUsuarioHistorialByPeriodosAndTenantAsync(IReadOnlyList<string> periodos, long tenantId) =>
            await (from h in this.DbContext.Set<UsuarioHistorial>().AsNoTracking()
                   where periodos.Contains(h.Periodo) && h.TenantId == tenantId
                   join u in this.DbContext.Set<Usuario>().AsNoTracking() on h.UsuarioId equals u.Id into uj
                   from u in uj.DefaultIfEmpty()
                   select new UsuarioHistorialReportProjection
                   {
                       Id = h.Id,
                       UsuarioId = h.UsuarioId,
                       TenantId = h.TenantId,
                       Periodo = h.Periodo,
                       FechaLastLogin = h.FechaLastLogin,
                       Nombre = u != null ? u.Nombre : null,
                       Apellido = u != null ? u.Apellido : null,
                       UserName = u != null ? u.UserName : null,
                       Email = u != null ? u.Email : null
                   }).ToListAsync();

        public Task<UsuarioHistorial> GetUltimoLoginByUserIdAsync(long usuarioId) =>
            this.DbContext.Set<UsuarioHistorial>()
                .AsNoTracking()
                .Where(x => x.UsuarioId == usuarioId)
                .OrderByDescending(x => x.FechaLastLogin)
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyDictionary<long, System.DateTime>> GetUltimosLoginsByUserIdsAsync(IReadOnlyList<long> usuarioIds)
        {
            if (usuarioIds.Count == 0)
            {
                return new Dictionary<long, System.DateTime>();
            }

            var data = await this.DbContext.Set<UsuarioHistorial>()
                .AsNoTracking()
                .Where(x => usuarioIds.Contains(x.UsuarioId))
                .GroupBy(x => x.UsuarioId)
                .Select(g => new { UsuarioId = g.Key, FechaUltimo = g.Max(h => h.FechaLastLogin) })
                .ToListAsync();

            return data.ToDictionary(x => x.UsuarioId, x => x.FechaUltimo);
        }

        public Task<Usuario> GetByUserNameWithAplicacionesAsync(string userName) =>
            this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .Include(x => x.UsuarioAplicaciones)
                .FirstOrDefaultAsync(x => x.UserName == userName);

        public Task<Usuario> GetByIdWithRolesAndAplicacionesAsync(long id) =>
            this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .Include(x => x.Roles).ThenInclude(x => x.Rol)
                .Include(x => x.UsuarioAplicaciones).ThenInclude(x => x.Aplicacion)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<Usuario> GetByIdWithRolesAsync(long id) =>
            this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .Include(x => x.Roles).ThenInclude(x => x.Rol)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<Usuario> GetByIdWithRolesAndAplicacionesForUpdateAsync(long id) =>
            this.DbContext.Set<Usuario>()
                .Include(x => x.Roles)
                .Include(x => x.UsuarioAplicaciones)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IReadOnlyList<long>> GetIdsNoAdministradoresAsync() =>
            await this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .Where(x => !x.Administrador)
                .Select(x => x.Id)
                .ToListAsync();

        public Task<PaginationSet<Usuario>> PaginateWithAplicacionesAsync(IListaPaginadaCriteriaBase criteria, bool excludeAdmin)
        {
            IQueryable<Usuario> query = this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .Include(x => x.UsuarioAplicaciones);

            if (excludeAdmin)
            {
                query = query.Where(u => !u.Administrador);
            }

            return this.BuildPaginationAsync(query, criteria);
        }

        public async Task<IReadOnlyList<long>> GetUsuarioIdsByRolIdAsync(long rolId) =>
            await this.DbContext.Set<UsuarioRol>()
                .AsNoTracking()
                .Where(x => x.RolId == rolId)
                .Select(x => x.UsuarioId)
                .Distinct()
                .ToListAsync();

        public async Task<IReadOnlyList<Usuario>> GetByRolIdsAsync(IReadOnlyList<long> rolIds) =>
            await this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .Include(x => x.Roles)
                .Where(x => x.Roles.Any(y => rolIds.Contains(y.RolId)))
                .ToListAsync();

        // Invalida la sesión de los NO-admin de un tenant al desactivarlo, en UNA sola sentencia SQL
        // (atómica, 1 round-trip): cambia el SecurityStamp → sus JWT vigentes fallan en OnTokenValidated
        // (ValidateSecurityStamp). Guid.NewGuid() lo traduce EF a NEWID() → un stamp único POR FILA.
        // IgnoreQueryFilters + scope explícito: la baja es root-only (contexto = tenant raíz) y el filtro
        // global multi-tenant ocultaría a los usuarios del tenant objetivo (hallazgo #20).
        public Task<int> BumpSecurityStampForNonAdminsAsync(long tenantId) =>
            this.DbContext.Set<Usuario>()
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantId && !u.Administrador)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(u => u.SecurityStamp, u => System.Guid.NewGuid().ToString()));

        // IgnoreQueryFilters: root consulta los admins de OTRO tenant desde el ABM; el filtro global
        // multi-tenant (TenantId == AppContext.TenantId) los ocultaría. "Admin" = usuario con el rol
        // default de nuevo tenant (el que recibe el admin creado en el alta).
        public async Task<IReadOnlyList<Usuario>> GetAdministradoresByTenantIdAsync(long tenantId) =>
            await this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantId
                            && u.Roles.Any(r => r.Rol.EsDefaultParaNuevoTenant))
                .ToListAsync();

        // IgnoreQueryFilters: impersonalización (ADR-0002). Root resuelve al usuario destino de OTRO tenant
        // (y a sí mismo al re-impersonar/parar); el filtro global multi-tenant lo ocultaría. El caller valida
        // el tenant del usuario explícitamente.
        public async Task<Usuario> GetByIdIgnoringTenantFilterAsync(long id) =>
            await this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);

        // IgnoreQueryFilters: todos los usuarios de OTRO tenant para el selector de impersonalización.
        public async Task<IReadOnlyList<Usuario>> GetByTenantIdIgnoringFilterAsync(long tenantId) =>
            await this.DbContext.Set<Usuario>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantId)
                .OrderBy(u => u.UserName)
                .ToListAsync();
    }
}
