using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IPermisoRepository : IEntityBaseRepository<Permiso>
    {
        /// <summary>
        /// Listado paginado proyectado a PermisoHeaderProjection (sin colecciones).
        /// AplicacionDescripcion + PermisoPadreDescripcion se resuelven en SQL via LEFT JOIN.
        /// Si excludeRestringidos=true, oculta los EsRestringido. Read-only.
        /// </summary>
        Task<PaginationSet<PermisoHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria, bool excludeRestringidos);

        /// <summary>Listado paginado. Si excludeRestringidos=true, oculta los EsRestringido.</summary>
        Task<PaginationSet<Permiso>> PaginateAsync(IListaPaginadaCriteriaBase criteria, bool excludeRestringidos);

        /// <summary>Listado completo. Si excludeRestringidos=true, oculta los EsRestringido.</summary>
        Task<IReadOnlyList<Permiso>> GetAllAsync(bool excludeRestringidos);

        /// <summary>Permiso por Id con Endpoints + Endpoint, tracked (para edición).</summary>
        Task<Permiso> GetByIdWithEndpointsAsync(long id);

        /// <summary>Permiso por Id con PermisoPadre + Endpoints + Endpoint. Read-only.</summary>
        Task<Permiso> GetByIdWithDetailsAsync(long id);

        /// <summary>Permisos con RolPermisos incluidos para construir árbol jerárquico. Si excludeRestringidos=true, oculta los EsRestringido. Read-only.</summary>
        Task<IReadOnlyList<Permiso>> GetWithRolPermisosAsync(bool excludeRestringidos);

        /// <summary>Permiso identificado por CodigoPermiso. Read-only.</summary>
        Task<Permiso> GetByCodigoPermisoAsync(string codigoPermiso);

        /// <summary>Ids de permisos asociados a alguno de los roles indicados.</summary>
        Task<IReadOnlyList<long>> GetPermisoIdsByRolIdsAsync(IReadOnlyList<long> rolIds);

        /// <summary>Ids de roles asociados a permisos cuyo CodigoPermiso esté en la lista.</summary>
        Task<IReadOnlyList<long>> GetRolIdsByCodigosPermisoAsync(IReadOnlyList<string> codigos);

        /// <summary>
        /// Permisos asociados a alguno de los roles indicados. Si codigosFilter no es null/empty,
        /// filtra también por CodigoPermiso ∈ codigosFilter. Read-only.
        /// </summary>
        Task<IReadOnlyList<Permiso>> GetByRolIdsAsync(IReadOnlyList<long> rolIds, IReadOnlyList<string> codigosFilter);
    }
}
