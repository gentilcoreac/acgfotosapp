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

    /// <summary>
    /// Items de pedidos de un evento en alguno de los <paramref name="estados"/> dados, con Foto,
    /// TamanoPrecio y Pedido.Participante materializados — insumo para la lista de impresión (el
    /// agrupado por foto+tamaño y por participante se arma en memoria en el AppService).
    /// </summary>
    Task<List<PedidoItem>> GetItemsParaImpresionAsync(long eventoId, IReadOnlyCollection<EstadoPedido> estados);
}
