using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Inputs;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/tipos-licencia")]
    public class TipoLicenciaController : ExtendedEntityApiControllerBase<TipoLicencia,
                                                                          TipoLicenciaInputDto,
                                                                          TipoLicenciaDto,
                                                                          TipoLicenciaHeaderDto,
                                                                          ListaPaginadaCriteriaBase,
                                                                          ITipoLicenciaAppService>
    {
        public TipoLicenciaController(ITipoLicenciaAppService tipoLicenciaAppService) : base(tipoLicenciaAppService)
        {
        }

        /// <summary>
        /// Cambia el flag EsDefaultParaNuevoTenant de un tipo de licencia. Endpoint dedicado,
        /// separado del Update normal: el TipoLicenciaInputDto NO expone el campo (defensa de
        /// mass assignment a nivel de tipo). Solo root (validación en AppService).
        /// </summary>
        [HttpPost]
        [Route("{id}/set-default-tenant")]
        public async Task<IActionResult> SetDefaultTenant(long id, [FromBody] SetDefaultTenantInput input)
        {
            await this.AppService.SetDefaultTenantAsync(id, input.EsDefaultParaNuevoTenant);
            return this.Ok();
        }
    }
}
