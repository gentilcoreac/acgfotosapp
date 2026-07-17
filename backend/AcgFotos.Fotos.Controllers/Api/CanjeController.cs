using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Security;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>Canje de código de acceso → token de sesión de familia (ADR-02, ADR-11). Sin registro.</summary>
    [ApiController]
    [Route("api/fotos/canje")]
    public class CanjeController : ApiControllerBase
    {
        private readonly ICanjeAppService _canjeAppService;

        public CanjeController(ICanjeAppService canjeAppService)
        {
            _canjeAppService = canjeAppService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CanjeResultDto), 200)]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingHelper.CanjePolicy)]
        public async Task<ActionResult<CanjeResultDto>> Canjear([FromBody] CanjeInputDto input)
        {
            var resultado = await _canjeAppService.CanjearAsync(input);
            return this.Ok(resultado);
        }
    }
}
