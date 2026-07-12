using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/aplicaciones")]
    public class AplicacionController : EntityApiControllerBase<Aplicacion,
                                                                AplicacionDto,
                                                                ListaPaginadaCriteriaBase,
                                                                IAplicacionAppService>
    {
        public AplicacionController(IAplicacionAppService aplicacionAppService) : base(aplicacionAppService)
        {
        }

        /// <summary>Devuelve las aplicaciones vinculadas al usuario logueado.</summary>
        [HttpGet]
        [Route("aplicaciones-permitidas")]
        public async Task<IActionResult> Get()
        {
            var dto = await this.AppService.GetAplicacionesPermitidasAsync();
            return this.Ok(dto);
        }

        [HttpGet]
        [Route("aplicaciones-tenant")]
        public async Task<IActionResult> GetAplicacionesPorTenant()
        {
            var dto = await this.AppService.GetAplicacionesPorTenantAsync();
            return this.Ok(dto);
        }

        [HttpGet]
        [Route("aplicaciones-tenant-id/{tenantId}")]
        public async Task<IActionResult> AplicacionesPorTenantId(long tenantId)
        {
            var dto = await this.AppService.GetAplicacionesPorTenantIdAsync(tenantId);
            return this.Ok(dto);
        }
    }
}
