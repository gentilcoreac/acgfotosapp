using AcgFotos.Core.Application;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.IServices;

/// <summary>
/// Admin de pedidos (Fase 2): listado por evento/estado, detalle y cambio de estado
/// (Pendiente/Pagado → Impreso → Entregado). Un pedido no se crea/edita/borra desde acá — lo
/// confirma la familia (<see cref="IFamiliaPedidoAppService"/>, ADR-07).
/// </summary>
public interface IPedidoAppService
{
    Task<PaginationSet<PedidoHeaderDto>> SearchAsync(PedidoCriteria criteria);

    Task<PedidoDetalleDto?> GetByIdAsync(long id);

    /// <summary>
    /// Cambia el estado y persiste. Permite cualquier estado (incluido volver atrás, corrección
    /// manual excepcional) salvo "cambiar" al mismo estado en el que ya está.
    /// </summary>
    Task<PedidoHeaderDto> CambiarEstadoAsync(long id, EstadoPedido nuevoEstado);
}
