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

    public Task<Grupo?> GetGrupoAsync(long grupoId) =>
        this.DbContext.Set<Grupo>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == grupoId);

    public Task<bool> ParticipantePerteneceAlGrupoAsync(long participanteId, long grupoId) =>
        this.DbContext.Set<Participante>().AsNoTracking().AnyAsync(a => a.Id == participanteId && a.GrupoId == grupoId);

    public Task<Foto?> GetByIdTrackedAsync(long id) =>
        this.DbContext.Set<Foto>().FirstOrDefaultAsync(f => f.Id == id);

    public Task<List<Foto>> ListarAsync(long grupoId, long? participanteId)
    {
        var query = this.DbContext.Set<Foto>().AsNoTracking().Where(f => f.GrupoId == grupoId);

        if (participanteId is not null)
        {
            query = query.Where(f => f.ParticipanteId == participanteId);
        }

        return query.OrderBy(f => f.Id).ToListAsync();
    }

    public Task<List<Foto>> GetPendientesTodosLosTenantsAsync() =>
        this.DbContext.Set<Foto>().AsNoTracking()
            .IgnoreQueryFilters() // barrido de arranque del worker: cruza tenants a propósito
            .Where(f => f.EstadoProcesamiento == EstadoProcesamientoFoto.Pendiente)
            .ToListAsync();

    public Task<List<Foto>> ListarParaFamiliaAsync(IReadOnlyCollection<long> participanteIds) =>
        this.VisiblesParaFamilia(participanteIds)
            .OrderBy(f => f.GrupoId).ThenBy(f => f.ParticipanteId).ThenBy(f => f.Id)
            .ToListAsync();

    public Task<Foto?> GetVisibleParaFamiliaAsync(long fotoId, IReadOnlyCollection<long> participanteIds) =>
        this.VisiblesParaFamilia(participanteIds).FirstOrDefaultAsync(f => f.Id == fotoId);

    /// <summary>
    /// Individuales de esos participantes + grupales (<c>ParticipanteId == null</c>) de sus grupos,
    /// Lista únicamente. El filtro global de tenant ya scopea por <c>IAppContext.TenantId</c>.
    /// </summary>
    private IQueryable<Foto> VisiblesParaFamilia(IReadOnlyCollection<long> participanteIds)
    {
        var gruposDeLaFamilia = this.DbContext.Set<Participante>().AsNoTracking()
            .Where(p => participanteIds.Contains(p.Id))
            .Select(p => p.GrupoId);

        return this.DbContext.Set<Foto>().AsNoTracking()
            .Where(f => f.EstadoProcesamiento == EstadoProcesamientoFoto.Lista
                && ((f.ParticipanteId != null && participanteIds.Contains(f.ParticipanteId.Value))
                    || (f.ParticipanteId == null && gruposDeLaFamilia.Contains(f.GrupoId))));
    }
}
