using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Criterias;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/parametros")]
    public class ParametroController : EntityApiControllerBase<Parametro,
                                                               ParametroDto,
                                                               ParametroCriteria,
                                                               IParametroAppService>
    {
        public ParametroController(IParametroAppService parametroAppService) : base(parametroAppService)
        {
        }

        /// <summary>
        /// Devuelve el valor del parametro nombrado. Si hay AppContext (usuario logueado)
        /// considera la aplicacion activa y posibles overrides por tenant. Si es anonimo,
        /// devuelve el default global del parametro (uso tipico: configurar UI antes del login,
        /// como OpcionesPaginacion).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [Route("valor-parametro-por-nombre/{nombre}")]
        public async Task<IActionResult> ValorParametroPorNombre(string nombre)
        {
            var valor = await this.AppService.ValorParametroPorNombreAsync(nombre);
            return this.Ok(valor);
        }

        [HttpGet]
        [Route("parametros-por-tenant-aplicacion")]
        public async Task<IActionResult> ParametrosPorTenantAplicacion([FromQuery] ParametrosTenantCriteria parametrosValorCriteria)
        {
            var result = await this.AppService.ParametrosPorTenantAplicacionAsync(parametrosValorCriteria);
            return this.Ok(result);
        }
    }
}
