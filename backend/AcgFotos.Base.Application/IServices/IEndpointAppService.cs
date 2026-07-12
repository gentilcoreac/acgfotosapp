using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Core.TreeView;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IEndpointAppService : IEntityAppServiceBase<Endpoint,
                                                                  EndpointDto,
                                                                  ListaPaginadaCriteriaBase>
    {
        Task<List<HierarchicalItem<long>>> GetHierarchicalItemsAsync();
    }
}
