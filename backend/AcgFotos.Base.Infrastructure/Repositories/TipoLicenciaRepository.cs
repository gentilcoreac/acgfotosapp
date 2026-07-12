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
    public class TipoLicenciaRepository : EntityBaseRepository<TipoLicencia>, ITipoLicenciaRepository
    {
        public TipoLicenciaRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        // Listado del ABM proyectado a TipoLicenciaHeaderProjection (proyección en SQL) + búsqueda
        // libre server-side (descripción/código).
        public Task<PaginationSet<TipoLicenciaHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.DbContext.Set<TipoLicencia>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(t =>
                    t.Descripcion.Contains(s) ||
                    t.CodigoTipoLicencia.Contains(s));
            }

            var projection = query
                .Select(t => new TipoLicenciaHeaderProjection
                {
                    Id = t.Id,
                    CodigoTipoLicencia = t.CodigoTipoLicencia,
                    Descripcion = t.Descripcion,
                    EsDefaultParaNuevoTenant = t.EsDefaultParaNuevoTenant
                });
            return this.BuildPaginationAsync(projection, criteria);
        }

        public Task<TipoLicencia> GetByIdWithRolesAsync(long id) =>
            this.DbContext.Set<TipoLicencia>()
                .Include(x => x.TipoLicenciaRoles)
                    .ThenInclude(x => x.Rol)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<TipoLicencia> GetByIdWithRolesReadOnlyAsync(long id) =>
            this.DbContext.Set<TipoLicencia>()
                .AsNoTracking()
                .Include(x => x.TipoLicenciaRoles)
                    .ThenInclude(x => x.Rol)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IReadOnlyList<TipoLicencia>> GetDefaultsParaNuevoTenantAsync() =>
            await this.DbContext.Set<TipoLicencia>()
                .Where(x => x.EsDefaultParaNuevoTenant)
                .ToListAsync();
    }
}
