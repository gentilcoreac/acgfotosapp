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
    public class TenantRepository : EntityBaseRepository<Tenant>, ITenantRepository
    {
        public TenantRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        private IQueryable<Tenant> ReadQuery() =>
            this.DbContext.Set<Tenant>().AsNoTracking();

        public Task<PaginationSet<TenantHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.ReadQuery();

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(t =>
                    t.Codigo.Contains(s) ||
                    t.Nombre.Contains(s) ||
                    t.TituloWeb.Contains(s) ||
                    t.HostName.Contains(s));
            }

            var projection = query
                .Select(t => new TenantHeaderProjection
                {
                    Id = t.Id,
                    Codigo = t.Codigo,
                    Nombre = t.Nombre,
                    TituloWeb = t.TituloWeb,
                    Activo = t.Activo,
                    HostName = t.HostName
                });
            return this.BuildPaginationAsync(projection, criteria);
        }

        public Task<Tenant> GetByIdReadOnlyAsync(long id) =>
            this.ReadQuery().FirstOrDefaultAsync(x => x.Id == id);

        public Task<Tenant> GetByHostNameOrCodigoAsync(string value)
        {
            var upper = value.ToUpper();
            return this.ReadQuery()
                .FirstOrDefaultAsync(x =>
                    x.HostName.ToUpper() == upper ||
                    x.Codigo.ToUpper() == upper);
        }

        public Task<Tenant> GetByIdWithLicensesAndAplicacionesAsync(long id) =>
            this.ReadQuery()
                .Include(x => x.TenantLicenses)
                .Include(x => x.TenantAplicaciones)
                    .ThenInclude(x => x.Aplicacion)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<Tenant> GetByIdForUpdateAsync(long id) =>
            this.DbContext.Set<Tenant>()
                .Include(x => x.TenantAplicaciones)
                .Include(x => x.TenantLicenses)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IReadOnlyList<Tenant>> GetTenantsExceptAsync(long tenantIdToExclude) =>
            await this.ReadQuery()
                .Where(x => x.Id != tenantIdToExclude)
                .ToListAsync();

        public async Task<IReadOnlyList<Tenant>> GetTenantsWithoutCustomThemeAsync() =>
            await this.ReadQuery()
                .Where(x => string.IsNullOrEmpty(x.StyleSheetCssUrl))
                .ToListAsync();

        public Task<bool> ExistsByNombreOrCodigoAsync(string nombre, string codigo) =>
            this.ReadQuery()
                .AnyAsync(x => x.Nombre == nombre || x.Codigo == codigo);

        public Task<PaginationSet<Tenant>> PaginateWithLicensesAndAplicacionesAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.ReadQuery()
                .Include(x => x.TenantLicenses)
                .Include(x => x.TenantAplicaciones)
                    .ThenInclude(x => x.Aplicacion);
            return this.BuildPaginationAsync(query, criteria);
        }

        public async Task<IReadOnlyList<Aplicacion>> GetAplicacionesByTenantIdAsync(long tenantId) =>
            await this.DbContext.Set<TenantAplicacion>()
                .AsNoTracking()
                .Include(x => x.Aplicacion)
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.Aplicacion)
                .ToListAsync();
    }
}
