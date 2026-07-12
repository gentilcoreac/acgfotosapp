using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IMenuRepository : IEntityBaseRepository<Menu>
    {
        /// <summary>Paginado con Permiso. Opcionalmente filtra por aplicacionId. Read-only.</summary>
        Task<PaginationSet<Menu>> PaginateWithPermisoAsync(IListaPaginadaCriteriaBase criteria, long? aplicacionId);

        /// <summary>Menús activos con un permiso específico. Opcionalmente filtra por aplicacionId. Read-only.</summary>
        Task<IReadOnlyList<Menu>> GetActivosByPermisoIdAsync(long permisoId, long? aplicacionId);

        /// <summary>
        /// Menús activos filtrados por aplicacionId. Incluye los que no requieren permiso (PermisoId null)
        /// o cuyo PermisoId esté en la lista provista. Read-only.
        /// </summary>
        Task<IReadOnlyList<Menu>> GetActivosByPermisosAsync(IReadOnlyList<long> permisoIds, long aplicacionId);
    }
}
