using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IAplicacionRepository : IEntityBaseRepository<Aplicacion>
    {
        /// <summary>
        /// Listado paginado proyectado a AplicacionHeaderProjection (id/código/nombre/activo) con
        /// búsqueda libre (searchText). La proyección ocurre en SQL. El AppService mapea a AplicacionDto.
        /// </summary>
        Task<PaginationSet<AplicacionHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>Permisos asociados a una aplicación. Read-only.</summary>
        Task<IReadOnlyList<Permiso>> GetPermisosByIdAsync(long aplicacionId);
    }
}
