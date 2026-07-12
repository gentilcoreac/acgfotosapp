using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

/// <summary>Repositorio EF del agregado <see cref="Curso"/> (el filtro global de tenant lo scopea por tenant).</summary>
public class CursoRepository : EntityBaseRepository<Curso>, ICursoRepository
{
    public CursoRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    private IQueryable<Curso> ReadQuery() => this.DbContext.Set<Curso>().AsNoTracking();

    public Task<PaginationSet<Curso>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria, long eventoId)
    {
        // Include de álbumes: el header muestra la cantidad, y los cursos de un evento son pocos.
        var query = this.ReadQuery().Include(c => c.Albumes).AsQueryable();

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

    public Task<Curso?> GetByIdWithAlbumesAsync(long id) =>
        this.DbContext.Set<Curso>()
            .Include(c => c.Albumes).ThenInclude(a => a.CodigosAcceso)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Curso?> GetByIdWithAlbumesReadOnlyAsync(long id) =>
        this.ReadQuery()
            .Include(c => c.Albumes).ThenInclude(a => a.CodigosAcceso)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<Curso>> GetAllWithAlbumesReadOnlyAsync() =>
        this.ReadQuery().Include(c => c.Albumes).ToListAsync();

    public Task<Curso?> GetByIdParaTarjetasAsync(long id) =>
        this.ReadQuery()
            .Include(c => c.Evento)
            .Include(c => c.Albumes).ThenInclude(a => a.CodigosAcceso)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExisteEventoAsync(long eventoId) =>
        this.DbContext.Set<Evento>().AsNoTracking().AnyAsync(e => e.Id == eventoId);

    public Task<List<long>> GetAlbumIdsConFotosAsync(long cursoId) =>
        this.DbContext.Set<Foto>().AsNoTracking()
            .Where(f => f.CursoId == cursoId && f.AlbumId != null)
            .Select(f => f.AlbumId!.Value)
            .Distinct()
            .ToListAsync();

    public Task<bool> TieneFotosAsync(long cursoId) =>
        this.DbContext.Set<Foto>().AsNoTracking().AnyAsync(f => f.CursoId == cursoId);
}
