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
    public class PermisoRepository : EntityBaseRepository<Permiso>, IPermisoRepository
    {
        public PermisoRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        private IQueryable<Permiso> ReadQuery(bool excludeRestringidos)
        {
            var query = this.DbContext.Set<Permiso>().AsNoTracking();
            return excludeRestringidos ? query.Where(p => !p.EsRestringido) : query;
        }

        public Task<PaginationSet<PermisoHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria, bool excludeRestringidos)
        {
            var query = this.ReadQuery(excludeRestringidos);

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(p =>
                    p.Nombre.Contains(s) ||
                    p.CodigoPermiso.Contains(s) ||
                    p.Descripcion.Contains(s));
            }

            var projection = query
                .Select(p => new PermisoHeaderProjection
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    CodigoPermiso = p.CodigoPermiso,
                    Descripcion = p.Descripcion,
                    Activo = p.Activo,
                    AplicacionDescripcion = p.Aplicacion == null ? "" : p.Aplicacion.Nombre,
                    PermisoPadreDescripcion = p.PermisoPadre == null ? "" : p.PermisoPadre.Nombre
                });
            return this.BuildPaginationAsync(projection, criteria);
        }

        public Task<PaginationSet<Permiso>> PaginateAsync(IListaPaginadaCriteriaBase criteria, bool excludeRestringidos) =>
            this.BuildPaginationAsync(this.ReadQuery(excludeRestringidos), criteria);

        public async Task<IReadOnlyList<Permiso>> GetAllAsync(bool excludeRestringidos) =>
            await this.ReadQuery(excludeRestringidos).ToListAsync();

        public Task<Permiso> GetByIdWithEndpointsAsync(long id) =>
            this.DbContext.Set<Permiso>()
                .Include(x => x.Endpoints)
                    .ThenInclude(y => y.Endpoint)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<Permiso> GetByIdWithDetailsAsync(long id) =>
            this.DbContext.Set<Permiso>()
                .AsNoTracking()
                .Include(x => x.PermisoPadre)
                .Include(x => x.Endpoints)
                    .ThenInclude(y => y.Endpoint)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IReadOnlyList<Permiso>> GetWithRolPermisosAsync(bool excludeRestringidos) =>
            await this.ReadQuery(excludeRestringidos)
                .Include(x => x.RolPermisos)
                .ToListAsync();

        public Task<Permiso> GetByCodigoPermisoAsync(string codigoPermiso) =>
            this.DbContext.Set<Permiso>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CodigoPermiso == codigoPermiso);

        public async Task<IReadOnlyList<long>> GetPermisoIdsByRolIdsAsync(IReadOnlyList<long> rolIds) =>
            await this.DbContext.Set<RolPermiso>()
                .AsNoTracking()
                .Where(x => rolIds.Contains(x.RolId))
                .Select(x => x.PermisoId)
                .Distinct()
                .ToListAsync();

        public async Task<IReadOnlyList<long>> GetRolIdsByCodigosPermisoAsync(IReadOnlyList<string> codigos) =>
            await this.DbContext.Set<Permiso>()
                .AsNoTracking()
                .Where(p => codigos.Contains(p.CodigoPermiso))
                .SelectMany(p => p.RolPermisos)
                .Select(rp => rp.RolId)
                .Distinct()
                .ToListAsync();

        public async Task<IReadOnlyList<Permiso>> GetByRolIdsAsync(IReadOnlyList<long> rolIds, IReadOnlyList<string> codigosFilter)
        {
            var query = this.DbContext.Set<Permiso>()
                .AsNoTracking()
                .Include(x => x.RolPermisos)
                .Where(x => x.RolPermisos.Any(y => rolIds.Contains(y.RolId)));

            if (codigosFilter != null && codigosFilter.Count > 0)
            {
                query = query.Where(x => codigosFilter.Contains(x.CodigoPermiso));
            }

            return await query.ToListAsync();
        }
    }
}
