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
    public class RolRepository : EntityBaseRepository<Rol>, IRolRepository
    {
        public RolRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<PaginationSet<RolHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.DbContext.Set<Rol>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(r => r.Descripcion.Contains(s));
            }

            var projection = query
                .Select(r => new RolHeaderProjection
                {
                    Id = r.Id,
                    Descripcion = r.Descripcion,
                    EsDefaultParaNuevoTenant = r.EsDefaultParaNuevoTenant
                });
            return this.BuildPaginationAsync(projection, criteria);
        }

        public Task<Rol> GetByIdWithRolPermisosAsync(long id) =>
            this.DbContext.Set<Rol>()
                .Include(x => x.RolPermisos)
                .FirstOrDefaultAsync(x => x.Id == id);

        public Task<Rol> GetByIdWithRolPermisosReadOnlyAsync(long id) =>
            this.DbContext.Set<Rol>()
                .AsNoTracking()
                .Include(x => x.RolPermisos)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IReadOnlyList<Rol>> GetWithRolPermisosAndPermisosAsync(string searchText = null)
        {
            IQueryable<Rol> query = this.DbContext.Set<Rol>()
                .AsNoTracking()
                .Include(x => x.RolPermisos)
                    .ThenInclude(x => x.Permiso);

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.Descripcion.Contains(searchText));
            }

            return await query.ToListAsync();
        }

        public async Task<IReadOnlyList<Rol>> GetByTipoLicenciaIdAsync(long tipoLicenciaId) =>
            await this.DbContext.Set<Rol>()
                .AsNoTracking()
                .Include(x => x.TipoLicenciaRoles)
                    .ThenInclude(x => x.TipoLicencia)
                .Where(x => x.TipoLicenciaRoles.Any(t => t.TipoLicenciaId == tipoLicenciaId))
                .ToListAsync();

        public async Task<IReadOnlyList<RolDelTenantProjection>> GetDelTenantConLicenciasAsync(long tenantId)
        {
            // TipoLicencias contratadas por el tenant (TenantLicencia es EntityBase → filtro explícito).
            var tipoLicenciaIds = this.DbContext.Set<TenantLicencia>()
                .AsNoTracking()
                .Where(tl => tl.TenantId == tenantId)
                .Select(tl => tl.TipoLicenciaId);

            // Roles que pertenecen a alguna de esas licencias (cada rol una vez; el Any no duplica),
            // con la(s) licencia(s) del tenant que lo incluyen para los chips (§11.1). Proyectado en SQL.
            return await this.DbContext.Set<Rol>()
                .AsNoTracking()
                .Where(r => r.TipoLicenciaRoles.Any(tlr => tipoLicenciaIds.Contains(tlr.TipoLicenciaId)))
                .OrderBy(r => r.Descripcion)
                .Select(r => new RolDelTenantProjection
                {
                    Id = r.Id,
                    Descripcion = r.Descripcion,
                    Licencias = r.TipoLicenciaRoles
                        .Where(tlr => tipoLicenciaIds.Contains(tlr.TipoLicenciaId))
                        .Select(tlr => new TipoLicenciaTagProjection
                        {
                            Id = tlr.TipoLicenciaId,
                            Descripcion = tlr.TipoLicencia.Descripcion
                        })
                        .OrderBy(t => t.Descripcion)
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Rol>> GetDefaultParaNuevoTenantAsync() =>
            await this.DbContext.Set<Rol>()
                .Where(x => x.EsDefaultParaNuevoTenant)
                .ToListAsync();
    }
}
