using AcgFotos.Fotos.Application.Dtos;

namespace AcgFotos.Fotos.Application.IServices;

/// <summary>
/// Carrito/pedido de la sesión de familia (Fase 2): catálogo de tamaños/precios del evento de la
/// sesión y confirmación del pedido. Mismo criterio de alcance que
/// <see cref="IFamiliaGaleriaAppService"/> — sale de los claims del JWT, nunca de un parámetro.
/// </summary>
public interface IFamiliaPedidoAppService
{
    /// <summary>Tamaños activos del evento de la sesión, ordenados por <c>Orden</c>.</summary>
    Task<List<TamanoPrecioDto>> ListarTamanosPreciosAsync();

    /// <summary>
    /// Confirma el pedido: valida que las fotos sean visibles para la sesión y los tamaños
    /// pertenezcan al catálogo activo del evento, congela el precio vigente y persiste.
    /// </summary>
    Task<PedidoConfirmadoDto> ConfirmarAsync(PedidoConfirmarInputDto input);
}
