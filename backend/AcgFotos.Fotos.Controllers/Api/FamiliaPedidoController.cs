using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Security;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>
    /// Carrito/pedido de la sesión de familia (ADR-11, Fase 2): catálogo de tamaños/precios del
    /// evento de la sesión y confirmación del pedido. Mismo criterio que
    /// <see cref="FamiliaGaleriaController"/> — cada acción requiere
    /// <see cref="AllowFamiliaSessionAttribute"/>, sin ella 403.
    /// </summary>
    [ApiController]
    [Route("api/fotos/familia")]
    public class FamiliaPedidoController : ApiControllerBase
    {
        private readonly IFamiliaPedidoAppService _familiaPedidoAppService;

        public FamiliaPedidoController(IFamiliaPedidoAppService familiaPedidoAppService)
        {
            _familiaPedidoAppService = familiaPedidoAppService;
        }

        [HttpGet]
        [Route("tamanos-precios")]
        [AllowFamiliaSession]
        public async Task<ActionResult<List<TamanoPrecioDto>>> GetTamanosPrecios()
        {
            return this.Ok(await _familiaPedidoAppService.ListarTamanosPreciosAsync());
        }

        [HttpPost]
        [Route("pedidos")]
        [AllowFamiliaSession]
        public async Task<ActionResult<PedidoConfirmadoDto>> Confirmar([FromBody] PedidoConfirmarInputDto input)
        {
            return this.Ok(await _familiaPedidoAppService.ConfirmarAsync(input));
        }
    }
}
