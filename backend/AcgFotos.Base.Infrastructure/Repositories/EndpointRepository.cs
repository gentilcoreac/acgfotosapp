using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class EndpointRepository : EntityBaseRepository<Endpoint>, IEndpointRepository
    {
        public EndpointRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<PaginationSet<Endpoint>> SearchByMatchAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.DbContext.Set<Endpoint>().AsNoTracking();

            if (!string.IsNullOrEmpty(criteria.SearchText))
            {
                var text = criteria.SearchText;
                query = query.Where(e =>
                    e.ModuleName.Contains(text) ||
                    e.ControllerName.Contains(text) ||
                    e.ActionName.Contains(text));
            }

            return this.BuildPaginationAsync(query, criteria);
        }

        public Task<bool> ExistsByActionControllerModuleAsync(string actionName, string controllerName, string moduleName) =>
            this.DbContext.Set<Endpoint>()
                .AnyAsync(x =>
                    x.ActionName == actionName &&
                    x.ControllerName == controllerName &&
                    x.ModuleName == moduleName);
    }
}
