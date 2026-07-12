using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

/// <summary>Repositorio EF de <see cref="Foto"/> (el filtro global de tenant lo scopea por tenant).</summary>
public class FotoRepository : EntityBaseRepository<Foto>, IFotoRepository
{
    public FotoRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    public Task<Curso?> GetCursoAsync(long cursoId) =>
        this.DbContext.Set<Curso>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == cursoId);

    public Task<bool> AlbumPerteneceAlCursoAsync(long albumId, long cursoId) =>
        this.DbContext.Set<Album>().AsNoTracking().AnyAsync(a => a.Id == albumId && a.CursoId == cursoId);

    public Task<Foto?> GetByIdTrackedAsync(long id) =>
        this.DbContext.Set<Foto>().FirstOrDefaultAsync(f => f.Id == id);

    public Task<List<Foto>> ListarAsync(long cursoId, long? albumId)
    {
        var query = this.DbContext.Set<Foto>().AsNoTracking().Where(f => f.CursoId == cursoId);

        if (albumId is not null)
        {
            query = query.Where(f => f.AlbumId == albumId);
        }

        return query.OrderBy(f => f.Id).ToListAsync();
    }

    public Task<List<Foto>> GetPendientesTodosLosTenantsAsync() =>
        this.DbContext.Set<Foto>().AsNoTracking()
            .IgnoreQueryFilters() // barrido de arranque del worker: cruza tenants a propósito
            .Where(f => f.EstadoProcesamiento == EstadoProcesamientoFoto.Pendiente)
            .ToListAsync();
}
