using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/logInfo")]
    public class LogInfoController : EntityApiControllerBase<LogInfo, 
                                                             LogInfoDto, 
                                                             ListaPaginadaCriteriaBase, 
                                                             ILogInfoAppService>
    {
        public LogInfoController(ILogInfoAppService logInfoAppService) : base(logInfoAppService)
        { }

        [HttpGet]
        [Route("AllTenants")]
        public ActionResult<PaginationSet<LogInfoAllOutput>> GetForAllTenants([FromQuery] LogInfoCriteria criteria)
        {
            return this.Ok(this.AppService.GetForAllTenants(criteria));
        }

        /// <summary>Detalle completo de un log por id (cross-tenant, root). Trae Exception/Properties.</summary>
        [HttpGet]
        [Route("{id}/all-tenants")]
        public ActionResult<LogInfoAllOutput> GetByIdForAllTenants(long id)
        {
            var dto = this.AppService.GetByIdForAllTenants(id);
            if (dto == null)
            {
                return this.NotFound();
            }
            return this.Ok(dto);
        }
    }
}
