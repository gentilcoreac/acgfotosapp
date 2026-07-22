using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

/// <summary>Repositorio EF del agregado <see cref="Pedido"/> (el filtro global de tenant lo scopea por tenant).</summary>
public class PedidoRepository : EntityBaseRepository<Pedido>, IPedidoRepository
{
    public PedidoRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    private IQueryable<Pedido> ReadQuery() => this.DbContext.Set<Pedido>().AsNoTracking();

    public Task<PaginationSet<Pedido>> PaginateHeadersAsync(
        ListaPaginadaCriteriaBase criteria, long eventoId, EstadoPedido? estado)
    {
        // Include de Participante+Grupo (evento del pedido) e Items (para CantidadItems, mismo
        // truco que Participantes.Count en GrupoRepository) — EF traduce todo a una sola query con
        // JOIN, Skip/Take incluido: pagina bien aunque haya cientos de pedidos, sin N+1.
        var query = this.ReadQuery()
            .Include(p => p.Participante).ThenInclude(a => a.Grupo)
            .Include(p => p.Items)
            .AsQueryable();

        if (eventoId != 0)
        {
            query = query.Where(p => p.Participante.Grupo.EventoId == eventoId);
        }

        if (estado != null)
        {
            query = query.Where(p => p.Estado == estado);
        }

        if (!string.IsNullOrEmpty(criteria.SearchText))
        {
            query = query.Where(p => p.NombreContacto.Contains(criteria.SearchText)
                                     || p.TelefonoContacto.Contains(criteria.SearchText)
                                     || p.Participante.Nombre.Contains(criteria.SearchText));
        }

        // Los más recientes primero por defecto (BuildPaginationAsync solo ordena si el caller pidió
        // un OrderBy explícito con el sort de la tabla).
        if (string.IsNullOrEmpty(criteria.OrderBy))
        {
            query = query.OrderByDescending(p => p.CreadoEn);
        }

        return this.BuildPaginationAsync(query, criteria);
    }

    public Task<Pedido?> GetByIdWithDetalleAsync(long id) =>
        this.ReadQuery()
            .Include(p => p.Participante).ThenInclude(a => a.Grupo)
            .Include(p => p.Items).ThenInclude(i => i.TamanoPrecio)
            .Include(p => p.Items).ThenInclude(i => i.Foto)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<PedidoItem>> GetItemsParaImpresionAsync(long eventoId, IReadOnlyCollection<EstadoPedido> estados) =>
        this.DbContext.Set<PedidoItem>()
            .AsNoTracking()
            .Include(i => i.Foto)
            .Include(i => i.TamanoPrecio)
            .Include(i => i.Pedido).ThenInclude(p => p.Participante)
            .Where(i => i.Pedido.Participante.Grupo.EventoId == eventoId && estados.Contains(i.Pedido.Estado))
            .ToListAsync();
}
