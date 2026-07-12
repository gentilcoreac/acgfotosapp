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
    public class ParametroRepository : EntityBaseRepository<Parametro>, IParametroRepository
    {
        public ParametroRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        private IQueryable<Parametro> ReadQueryWithAplicacion() =>
            this.DbContext.Set<Parametro>()
                .AsNoTracking()
                .Include(x => x.Aplicacion);

        public Task<PaginationSet<Parametro>> PaginateWithAplicacionAsync(IListaPaginadaCriteriaBase criteria, long? aplicacionId)
        {
            var query = this.ReadQueryWithAplicacion();
            if (aplicacionId.HasValue && aplicacionId.Value != 0)
            {
                query = query.Where(x => x.AplicacionId == aplicacionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(x =>
                    x.Nombre.Contains(s) ||
                    x.Descripcion.Contains(s));
            }

            return this.BuildPaginationAsync(query, criteria);
        }

        public Task<Parametro> GetByIdWithAplicacionAsync(long id) =>
            this.ReadQueryWithAplicacion()
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IReadOnlyList<Parametro>> GetAllWithAplicacionAsync() =>
            await this.ReadQueryWithAplicacion().ToListAsync();

        public async Task<IReadOnlyList<Parametro>> GetByAplicacionIdAsync(long aplicacionId) =>
            await this.ReadQueryWithAplicacion()
                .Where(x => x.AplicacionId == aplicacionId)
                .ToListAsync();

        public Task<Parametro> GetByNombreAndAplicacionAsync(string nombre, long aplicacionId) =>
            this.DbContext.Set<Parametro>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Nombre == nombre && x.AplicacionId == aplicacionId);

        public Task<Parametro> GetByNombreAsync(string nombre) =>
            this.DbContext.Set<Parametro>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Nombre == nombre);

        public async Task<IReadOnlyList<ParametroValorTenant>> GetValoresByTenantIdAsync(long tenantId) =>
            await this.DbContext.Set<ParametroValorTenant>()
                .AsNoTracking()
                .Include(x => x.Parametro)
                .Where(x => x.TenantId == tenantId)
                .ToListAsync();

        public Task<PaginationSet<ParametroValorTenant>> PaginateValoresAsync(IListaPaginadaCriteriaBase criteria, long? tenantId)
        {
            // ParametroValorTenant NO es IMultiTenantEntityBase (no hay filtro global): el aislamiento de
            // NO-root es EXPLICITO aca. tenantId=null => root ve todos (admin centralizado).
            var query = this.DbContext.Set<ParametroValorTenant>().AsNoTracking();
            if (tenantId.HasValue)
            {
                query = query.Where(x => x.TenantId == tenantId.Value);
            }
            return this.BuildPaginationAsync(query, criteria);
        }
    }
}
