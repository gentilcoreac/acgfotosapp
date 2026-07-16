using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

/// <summary>Repositorio EF del agregado <see cref="Grupo"/> (el filtro global de tenant lo scopea por tenant).</summary>
public class GrupoRepository : EntityBaseRepository<Grupo>, IGrupoRepository
{
    public GrupoRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    private IQueryable<Grupo> ReadQuery() => this.DbContext.Set<Grupo>().AsNoTracking();

    public Task<PaginationSet<Grupo>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria, long eventoId)
    {
        // Include de participantes: el header muestra la cantidad, y los grupos de un evento son pocos.
        var query = this.ReadQuery().Include(c => c.Participantes).AsQueryable();

        if (eventoId != 0)
        {
            query = query.Where(c => c.EventoId == eventoId);
        }

        if (!string.IsNullOrEmpty(criteria.SearchText))
        {
            query = query.Where(c => c.Nombre.Contains(criteria.SearchText));
        }

        return this.BuildPaginationAsync(query, criteria);
    }

    public Task<Grupo?> GetByIdWithParticipantesAsync(long id) =>
        this.DbContext.Set<Grupo>()
            .Include(c => c.Participantes).ThenInclude(a => a.CodigosAcceso)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Grupo?> GetByIdWithParticipantesReadOnlyAsync(long id) =>
        this.ReadQuery()
            .Include(c => c.Participantes).ThenInclude(a => a.CodigosAcceso)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<Grupo>> GetAllWithParticipantesReadOnlyAsync() =>
        this.ReadQuery().Include(c => c.Participantes).ToListAsync();

    public Task<Grupo?> GetByIdParaTarjetasAsync(long id) =>
        this.ReadQuery()
            .Include(c => c.Evento)
            .Include(c => c.Participantes).ThenInclude(a => a.CodigosAcceso)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExisteEventoAsync(long eventoId) =>
        this.DbContext.Set<Evento>().AsNoTracking().AnyAsync(e => e.Id == eventoId);

    public Task<List<long>> GetParticipanteIdsConFotosAsync(long grupoId) =>
        this.DbContext.Set<Foto>().AsNoTracking()
            .Where(f => f.GrupoId == grupoId && f.ParticipanteId != null)
            .Select(f => f.ParticipanteId!.Value)
            .Distinct()
            .ToListAsync();

    public Task<bool> TieneFotosAsync(long grupoId) =>
        this.DbContext.Set<Foto>().AsNoTracking().AnyAsync(f => f.GrupoId == grupoId);
}
