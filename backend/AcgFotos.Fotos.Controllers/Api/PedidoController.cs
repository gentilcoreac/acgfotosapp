using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;

namespace AcgFotos.Fotos.Controllers.Api
{
    /// <summary>
    /// Admin de pedidos (Fase 2): listado por evento/estado, detalle y cambio de estado. No hereda
    /// <c>ExtendedEntityApiControllerBase</c> porque no hay create/update/delete (ADR-07: el pedido
    /// lo confirma la familia).
    /// </summary>
    [ApiController]
    [Route("api/fotos/pedidos")]
    public class PedidoController : ApiControllerBase
    {
        private readonly IPedidoAppService _pedidoAppService;

        public PedidoController(IPedidoAppService pedidoAppService)
        {
            _pedidoAppService = pedidoAppService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginationSet<PedidoHeaderDto>>> Search([FromQuery] PedidoCriteria criteria)
        {
            return this.Ok(await _pedidoAppService.SearchAsync(criteria));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<PedidoDetalleDto>> GetById(long id)
        {
            var pedido = await _pedidoAppService.GetByIdAsync(id);
            return pedido == null ? this.NotFound() : this.Ok(pedido);
        }

        [HttpPost]
        [Route("{id}/estado")]
        public async Task<ActionResult<PedidoHeaderDto>> CambiarEstado(long id, [FromBody] PedidoCambiarEstadoInputDto input)
        {
            return this.Ok(await _pedidoAppService.CambiarEstadoAsync(id, input.Estado));
        }
    }
}
