using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Criterias;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IMenuAppService : IEntityAppServiceBase<Menu,
                                                             MenuDto,
                                                             MenuCriteria>
    {
        Task<List<MenuDto>> ObtenerMenuAsideAsync();
        Task<List<MenuDashDto>> ObtenerMenusDashAsync();
        Task<List<AllowedRouteOutput>> ObtenerAllowedRoutesAsync();
    }
}
