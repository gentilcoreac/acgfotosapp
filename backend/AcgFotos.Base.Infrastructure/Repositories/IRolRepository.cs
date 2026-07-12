using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IRolRepository : IEntityBaseRepository<Rol>
    {
        /// <summary>Listado paginado proyectado a RolHeaderProjection (sin RolPermisos). Read-only.</summary>
        Task<PaginationSet<RolHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>Rol con sus RolPermisos. Tracked (para edición).</summary>
        Task<Rol> GetByIdWithRolPermisosAsync(long id);

        /// <summary>Rol con sus RolPermisos. Read-only.</summary>
        Task<Rol> GetByIdWithRolPermisosReadOnlyAsync(long id);

        /// <summary>
        /// Roles con RolPermisos + Permiso (para verificación cross-aplicacion). Read-only.
        /// Si searchText viene, filtra por Descripcion en SQL (case-insensitive en collation por defecto).
        /// </summary>
        Task<IReadOnlyList<Rol>> GetWithRolPermisosAndPermisosAsync(string searchText = null);

        /// <summary>Roles asociados a un TipoLicencia, con TipoLicenciaRoles + TipoLicencia incluidos. Read-only.</summary>
        Task<IReadOnlyList<Rol>> GetByTipoLicenciaIdAsync(long tipoLicenciaId);

        /// <summary>
        /// Roles disponibles para un tenant según sus licencias contratadas
        /// (gen_TenantLicencias → TipoLicencia → gen_TipoLicenciaRoles), ordenados por descripción,
        /// y por cada rol la(s) licencia(s) del tenant que lo incluyen (para los chips del ABM de
        /// grupos, §11.1). Excluye roles huérfanos (sin licencia) y los de licencias que el tenant no
        /// tiene; los chips también se limitan a las licencias del tenant. Proyectado en SQL, read-only.
        /// </summary>
        Task<IReadOnlyList<RolDelTenantProjection>> GetDelTenantConLicenciasAsync(long tenantId);

        /// <summary>Roles marcados como default para nuevos tenants. Tracked (los callers crean UsuarioRoles vinculados).</summary>
        Task<IReadOnlyList<Rol>> GetDefaultParaNuevoTenantAsync();
    }
}
