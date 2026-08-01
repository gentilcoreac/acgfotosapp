using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

public class OpcionesPublicacionRepository : EntityBaseRepository<OpcionesPublicacion>, IOpcionesPublicacionRepository
{
    public OpcionesPublicacionRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    public Task<OpcionesPublicacion?> GetDefaultReadOnlyAsync() =>
        this.DbContext.Set<OpcionesPublicacion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.EsDefault);
}
