using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

/// <summary>Repositorio EF del agregado <see cref="Evento"/> (el filtro global de tenant lo scopea por tenant).</summary>
public class EventoRepository : EntityBaseRepository<Evento>, IEventoRepository
{
    public EventoRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    private IQueryable<Evento> ReadQuery() => this.DbContext.Set<Evento>().AsNoTracking();

    public Task<PaginationSet<Evento>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria)
    {
        var query = this.ReadQuery();

        if (!string.IsNullOrEmpty(criteria.SearchText))
        {
            query = query.Where(e => e.Nombre.Contains(criteria.SearchText)
                                     || (e.Colegio != null && e.Colegio.Contains(criteria.SearchText)));
        }

        return this.BuildPaginationAsync(query, criteria);
    }

    public Task<Evento?> GetByIdWithTamanosAsync(long id) =>
        this.DbContext.Set<Evento>()
            .Include(e => e.TamanosPrecios)
            .FirstOrDefaultAsync(e => e.Id == id);

    public Task<Evento?> GetByIdWithTamanosReadOnlyAsync(long id) =>
        this.ReadQuery()
            .Include(e => e.TamanosPrecios)
            .FirstOrDefaultAsync(e => e.Id == id);
}
