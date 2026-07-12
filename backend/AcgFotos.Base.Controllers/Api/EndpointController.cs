using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/endpoints")]
    public class EndpointController : EntityApiControllerBase<Endpoint,
                                                              EndpointDto,
                                                              ListaPaginadaCriteriaBase,
                                                              IEndpointAppService>
    {
        public EndpointController(IEndpointAppService endpointAppService) : base(endpointAppService)
        {
        }

        [Route("hierarchical-items")]
        [HttpGet]
        public async Task<IActionResult> GetHierarchicalItems()
        {
            var result = await this.AppService.GetHierarchicalItemsAsync();
            return this.Ok(result);
        }
    }
}
