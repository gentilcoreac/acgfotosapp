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
    public class MenuRepository : EntityBaseRepository<Menu>, IMenuRepository
    {
        public MenuRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<PaginationSet<Menu>> PaginateWithPermisoAsync(IListaPaginadaCriteriaBase criteria, long? aplicacionId)
        {
            IQueryable<Menu> query = this.DbContext.Set<Menu>()
                .AsNoTracking()
                .Include(x => x.Permiso)
                .Include(x => x.Aplicacion);

            if (aplicacionId.HasValue && aplicacionId.Value != 0)
            {
                query = query.Where(x => x.AplicacionId == aplicacionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(x =>
                    x.Nombre.Contains(s) ||
                    x.Codigo.Contains(s));
            }

            return this.BuildPaginationAsync(query, criteria);
        }

        public async Task<IReadOnlyList<Menu>> GetActivosByPermisoIdAsync(long permisoId, long? aplicacionId)
        {
            var query = this.DbContext.Set<Menu>()
                .AsNoTracking()
                .Where(x => x.Estado && x.PermisoId == permisoId);

            if (aplicacionId.HasValue)
            {
                query = query.Where(x => x.AplicacionId == aplicacionId.Value);
            }

            return await query.OrderBy(x => x.Id).ToListAsync();
        }

        public async Task<IReadOnlyList<Menu>> GetActivosByPermisosAsync(IReadOnlyList<long> permisoIds, long aplicacionId) =>
            await this.DbContext.Set<Menu>()
                .AsNoTracking()
                .Where(x => x.Estado &&
                            x.AplicacionId == aplicacionId &&
                            (!x.PermisoId.HasValue ||
                             (x.PermisoId.HasValue && permisoIds.Contains(x.PermisoId.Value))))
                .OrderBy(x => x.Id)
                .ToListAsync();
    }
}
