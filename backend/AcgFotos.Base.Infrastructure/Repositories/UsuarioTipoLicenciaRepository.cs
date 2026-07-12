using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class UsuarioTipoLicenciaRepository : EntityBaseRepository<UsuarioTipoLicencia>, IUsuarioTipoLicenciaRepository
    {
        public UsuarioTipoLicenciaRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public async Task<IReadOnlyDictionary<long, int>> GetCountActivasPorTipoByTenantAsync(long tenantId)
        {
            // IgnoreQueryFilters: el conteo ya scopea EXPLÍCITAMENTE por `x.Usuario.TenantId == tenantId`,
            // así que el filtro global multi-tenant es redundante y, peor, ROMPE el caso cross-tenant: cuando
            // ROOT (contexto = tenant raíz) valida el tope de licencias de OTRO tenant (ValidateTenantLicensesAsync
            // en /tenants/update), el filtro global acotaría a su propio tenant → contaría 0 asignadas → el tope
            // nunca dispararía (root es el único caller). Sin el filtro, el conteo es el del tenant objetivo.
            var dict = await this.DbContext.Set<UsuarioTipoLicencia>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => x.Usuario.TenantId == tenantId && x.IsActive)
                .GroupBy(x => x.TipoLicenciaId)
                .Select(g => new { TipoLicenciaId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.TipoLicenciaId, x => x.Cantidad);
            return dict;
        }

        public async Task<DateTime?> GetActiveLicenseExpirationByUserNameAndTenantAsync(string userName, long tenantId)
        {
            var data = await (from utl in this.DbContext.Set<UsuarioTipoLicencia>().AsNoTracking()
                              join tl in this.DbContext.Set<TenantLicencia>().AsNoTracking()
                                  on new { utl.TipoLicenciaId, TenantId = tenantId } equals new { tl.TipoLicenciaId, tl.TenantId }
                              where utl.Usuario.UserName == userName && utl.IsActive
                              select new { tl.ExpireDatetime })
                              .FirstOrDefaultAsync();
            return data?.ExpireDatetime;
        }

        public async Task<IReadOnlyList<long>> GetUsuarioIdsWithActiveLicenseByTenantAsync(long tenantId) =>
            await this.DbContext.Set<UsuarioTipoLicencia>()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive)
                .Select(x => x.UsuarioId)
                .Distinct()
                .ToListAsync();

        public Task<UsuarioTipoLicencia> GetActiveByUserIdAsync(long userId) =>
            this.DbContext.Set<UsuarioTipoLicencia>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.UsuarioId == userId);

        public async Task<IReadOnlyList<UsuarioTipoLicencia>> GetActivesByUserIdsAsync(IReadOnlyList<long> userIds)
        {
            if (userIds.Count == 0)
            {
                return new List<UsuarioTipoLicencia>();
            }
            return await this.DbContext.Set<UsuarioTipoLicencia>()
                .AsNoTracking()
                .Where(x => x.IsActive && userIds.Contains(x.UsuarioId))
                .ToListAsync();
        }
    }
}
