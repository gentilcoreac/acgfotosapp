using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IEndpointRepository : IEntityBaseRepository<Endpoint>
    {
        /// <summary>Paginado con búsqueda parcial por ModuleName, ControllerName o ActionName.</summary>
        Task<PaginationSet<Endpoint>> SearchByMatchAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>True si existe un endpoint con la tupla module/controller/action.</summary>
        Task<bool> ExistsByActionControllerModuleAsync(string actionName, string controllerName, string moduleName);
    }
}
