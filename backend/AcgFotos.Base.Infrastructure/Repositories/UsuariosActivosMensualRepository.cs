using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class UsuariosActivosMensualRepository : EntityBaseRepository<UsuariosActivosMensual>, IUsuariosActivosMensualRepository
    {
        public UsuariosActivosMensualRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public async Task<IReadOnlyList<UsuariosActivosMensual>> GetByTenantIdAsync(long tenantId) =>
            await this.DbContext.Set<UsuariosActivosMensual>()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .ToListAsync();

        public Task<UsuariosActivosMensual> GetByPeriodoAndTenantIdAsync(string periodo, long tenantId) =>
            this.DbContext.Set<UsuariosActivosMensual>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Periodo == periodo && x.TenantId == tenantId);

        public Task<PaginationSet<UsuariosActivosMensual>> PaginateByTenantWithSearchAsync(IListaPaginadaCriteriaBase criteria, long tenantId)
        {
            var query = this.DbContext.Set<UsuariosActivosMensual>()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId);

            if (!string.IsNullOrEmpty(criteria.SearchText))
            {
                var search = criteria.SearchText;
                query = query.Where(x => x.Periodo.Contains(search));
            }

            return this.BuildPaginationAsync(query, criteria);
        }
    }
}
