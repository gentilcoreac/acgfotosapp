using Microsoft.EntityFrameworkCore;
using AcgFotos.Core.Data;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Infrastructure.Persistence.Ef.Repositories;

/// <summary>Repositorio EF de <see cref="CodigoAcceso"/>. El canje corre ANTES de conocer el tenant.</summary>
public class CodigoAccesoRepository : EntityBaseRepository<CodigoAcceso>, ICodigoAccesoRepository
{
    public CodigoAccesoRepository(IDbContext dbContext, IAppContext appContext)
        : base(dbContext, appContext)
    {
    }

    // IgnoreQueryFilters: el canje llega SIN token (o con uno de otra sesión) — todavía no hay
    // tenant conocido, y el filtro global de EF scopearía por AppContext.TenantId (0 acá). El
    // código en sí es la única credencial: su índice único (TenantId, Codigo) alcanza para no
    // pisar códigos de otros tenants (mismo patrón que FotoRepository.cs, barrido cross-tenant).
    public Task<CodigoAcceso?> GetVigenteConEventoAsync(string codigo) =>
        this.DbContext.Set<CodigoAcceso>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.Participante).ThenInclude(p => p.Grupo).ThenInclude(g => g.Evento)
            .FirstOrDefaultAsync(c => c.Codigo == codigo);
}
