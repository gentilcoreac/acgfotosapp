using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface ITenantLicenciaRepository : IEntityBaseRepository<TenantLicencia>
    {
        /// <summary>
        /// TenantLicencias de un tenant con TipoLicencia incluido. Read-only.
        /// Si onlyActive es true, filtra las expiradas en SQL (ExpireDatetime > now).
        /// </summary>
        Task<IReadOnlyList<TenantLicencia>> GetByTenantWithTipoLicenciaAsync(long tenantId, bool onlyActive);
    }
}
