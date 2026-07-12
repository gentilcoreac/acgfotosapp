using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface ITipoLicenciaRepository : IEntityBaseRepository<TipoLicencia>
    {
        /// <summary>
        /// Listado paginado proyectado a TipoLicenciaHeaderProjection con búsqueda libre (searchText).
        /// La proyección ocurre en SQL. El AppService mapea a TipoLicenciaHeaderDto.
        /// </summary>
        Task<PaginationSet<TipoLicenciaHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>TipoLicencia con TipoLicenciaRoles + Rol incluidos, tracked.</summary>
        Task<TipoLicencia> GetByIdWithRolesAsync(long id);

        /// <summary>TipoLicencia con TipoLicenciaRoles + Rol incluidos, AsNoTracking (lectura/detalle).</summary>
        Task<TipoLicencia> GetByIdWithRolesReadOnlyAsync(long id);

        /// <summary>TipoLicencias marcadas como default para tenant nuevo. Tracked (el caller las usa para asignar licencias).</summary>
        Task<IReadOnlyList<TipoLicencia>> GetDefaultsParaNuevoTenantAsync();
    }
}
