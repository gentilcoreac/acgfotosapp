using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class GrupoRepository : EntityBaseRepository<Grupo>, IGrupoRepository
    {
        public GrupoRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<PaginationSet<GrupoHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.DbContext.Set<Grupo>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(g => g.Nombre.Contains(s));
            }

            var projection = query
                .Select(g => new GrupoHeaderProjection
                {
                    Id = g.Id,
                    Nombre = g.Nombre,
                    // COUNT en SQL (correlated subquery); el filtro global multi-tenant aplica a UsuarioGrupos.
                    CantidadMiembros = g.UsuarioGrupos.Count()
                });
            return this.BuildPaginationAsync(projection, criteria);
        }

        public Task<Grupo> GetByIdWithUsuariosAsync(long id) =>
            this.DbContext.Set<Grupo>()
                .Include(x => x.UsuarioGrupos)
                .Include(x => x.GrupoRoles)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<Grupo> GetByIdWithUsuariosReadOnlyAsync(long id) =>
            this.DbContext.Set<Grupo>()
                .AsNoTracking()
                .Include(x => x.UsuarioGrupos).ThenInclude(ug => ug.Usuario)
                .Include(x => x.GrupoRoles)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<Dictionary<long, long>> GetActiveTipoLicenciaIdByUsuarioIdsAsync(IReadOnlyCollection<long> usuarioIds)
        {
            if (usuarioIds == null || usuarioIds.Count == 0)
            {
                return new Dictionary<long, long>();
            }

            return await this.DbContext.Set<UsuarioTipoLicencia>()
                .AsNoTracking()
                .Where(l => l.IsActive && usuarioIds.Contains(l.UsuarioId))
                .Select(l => new { l.UsuarioId, l.TipoLicenciaId })
                .ToDictionaryAsync(x => x.UsuarioId, x => x.TipoLicenciaId);
        }
    }
}
