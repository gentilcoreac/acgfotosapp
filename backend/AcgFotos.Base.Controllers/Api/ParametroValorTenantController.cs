using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    [Route("api/general/parametros-valores")]
    public class ParametroValorTenantController : EntityApiControllerBase<ParametroValorTenant,
                                                                          ParametroValorTenantDto,
                                                                          ListaPaginadaCriteriaBase,
                                                                          IParametroValorTenantAppService>
    {
        public ParametroValorTenantController(IParametroValorTenantAppService parametroValorAppService) : base(parametroValorAppService)
        {
        }


        /// <summary>
        /// Este endpoint considera el valor custom para el tenant y aplicacion del contexto.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("valorizar")]
        public async Task<IActionResult> ValorizarParametro(ParametroValorValorizarInput input)
        {
            await this.AppService.ValorizarParametroAsync(input);
            return this.Ok();
        }

    }
}
