using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

public class PerfilMarcaAguaRepository : EntityBaseRepository<PerfilMarcaAgua>, IPerfilMarcaAguaRepository
{
    public PerfilMarcaAguaRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    private IQueryable<PerfilMarcaAgua> ReadQuery() => this.DbContext.Set<PerfilMarcaAgua>().AsNoTracking();

    public Task<PerfilMarcaAgua?> GetByIdConCapasReadOnlyAsync(long id) =>
        this.ReadQuery()
            .Include(p => p.Capas)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<PerfilMarcaAgua?> GetDefaultConCapasReadOnlyAsync() =>
        this.ReadQuery()
            .Include(p => p.Capas)
            .FirstOrDefaultAsync(p => p.EsDefault);

    public async Task<IReadOnlyList<PerfilMarcaAgua>> GetAllConCapasReadOnlyAsync() =>
        await this.ReadQuery().Include(p => p.Capas).ToListAsync();

    public Task<PerfilMarcaAgua?> GetByIdConCapasAsync(long id) =>
        this.DbContext.Set<PerfilMarcaAgua>()
            .Include(p => p.Capas)
            .FirstOrDefaultAsync(p => p.Id == id);
}
