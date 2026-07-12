using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IUsuariosActivosMensualRepository : IEntityBaseRepository<UsuariosActivosMensual>
    {
        Task<IReadOnlyList<UsuariosActivosMensual>> GetByTenantIdAsync(long tenantId);

        Task<UsuariosActivosMensual> GetByPeriodoAndTenantIdAsync(string periodo, long tenantId);

        /// <summary>
        /// Listado paginado por tenant. Si criteria.SearchText viene, filtra por Periodo en SQL.
        /// Orden + Skip/Take aplicados en SQL via BuildPaginationAsync. Read-only.
        /// </summary>
        Task<PaginationSet<UsuariosActivosMensual>> PaginateByTenantWithSearchAsync(IListaPaginadaCriteriaBase criteria, long tenantId);
    }
}
