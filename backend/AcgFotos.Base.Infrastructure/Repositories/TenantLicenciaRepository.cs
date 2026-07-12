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
    public class TenantLicenciaRepository : EntityBaseRepository<TenantLicencia>, ITenantLicenciaRepository
    {
        public TenantLicenciaRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public async Task<IReadOnlyList<TenantLicencia>> GetByTenantWithTipoLicenciaAsync(long tenantId, bool onlyActive)
        {
            var query = this.DbContext.Set<TenantLicencia>()
                .AsNoTracking()
                .Include(x => x.TipoLicencia)
                .Where(x => x.TenantId == tenantId);

            if (onlyActive)
            {
                var now = DateTime.Now;
                query = query.Where(x => x.ExpireDatetime > now);
            }

            return await query.ToListAsync();
        }
    }
}
