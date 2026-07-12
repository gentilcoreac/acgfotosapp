using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Criterias;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/menus")]
    public class MenuController : EntityApiControllerBase<Menu,
                                                          MenuDto,
                                                          MenuCriteria,
                                                          IMenuAppService>
    {
        public MenuController(IMenuAppService menuAppService) : base(menuAppService)
        {
        }

        [HttpGet]
        [Route("principal")]
        public async Task<ActionResult<List<MenuDto>>> Get()
        {
            var result = await this.AppService.ObtenerMenuAsideAsync();
            return this.Ok(result);
        }

        [HttpGet]
        [Route("dashboard")]
        public async Task<ActionResult<List<MenuDashDto>>> GetMenusDash()
        {
            var result = await this.AppService.ObtenerMenusDashAsync();
            return this.Ok(result);
        }

        [HttpGet]
        [Route("allowed-routes")]
        public async Task<ActionResult<List<AllowedRouteOutput>>> GetAllowedRoutes()
        {
            var result = await this.AppService.ObtenerAllowedRoutesAsync();
            return this.Ok(result);
        }
    }
}
