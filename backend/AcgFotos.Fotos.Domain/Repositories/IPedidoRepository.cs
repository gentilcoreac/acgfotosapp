using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IPedidoRepository : IEntityBaseRepository<Pedido>
{
    /// <summary>
    /// Listado admin: filtra por evento (0 = todos), estado (null = todos) y SearchText
    /// (contacto/participante), y pagina. Mismo criterio que <see cref="IGrupoRepository"/>: el
    /// criteria específico del vertical (con EventoId/Estado) vive en Application, así que acá
    /// llegan como parámetros sueltos sobre el <see cref="ListaPaginadaCriteriaBase"/> genérico.
    /// </summary>
    Task<PaginationSet<Pedido>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria, long eventoId, EstadoPedido? estado);

    /// <summary>Detalle read-only con las líneas y su tamaño (para GetById).</summary>
    Task<Pedido?> GetByIdWithDetalleAsync(long id);
}
