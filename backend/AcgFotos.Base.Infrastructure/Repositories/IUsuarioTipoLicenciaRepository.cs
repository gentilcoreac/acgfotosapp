using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IUsuarioTipoLicenciaRepository : IEntityBaseRepository<UsuarioTipoLicencia>
    {
        /// <summary>Cantidad de licencias activas por TipoLicenciaId para un tenant. Single query agrupada (evita N+1).</summary>
        Task<IReadOnlyDictionary<long, int>> GetCountActivasPorTipoByTenantAsync(long tenantId);

        /// <summary>
        /// Fecha de expiración de la TenantLicencia del tipo activo de un usuario en un tenant.
        /// JOIN entre UsuarioTipoLicencia y TenantLicencia. Devuelve null si el usuario no tiene
        /// licencia activa o la TenantLicencia no existe para ese tipo.
        /// </summary>
        Task<DateTime?> GetActiveLicenseExpirationByUserNameAndTenantAsync(string userName, long tenantId);

        /// <summary>Ids de usuarios con al menos una licencia activa en el tenant indicado.</summary>
        Task<IReadOnlyList<long>> GetUsuarioIdsWithActiveLicenseByTenantAsync(long tenantId);

        /// <summary>Licencia activa de un usuario. Read-only.</summary>
        Task<UsuarioTipoLicencia> GetActiveByUserIdAsync(long userId);

        /// <summary>Licencias activas para un set de usuarios. Single query (evita N+1). Read-only.</summary>
        Task<IReadOnlyList<UsuarioTipoLicencia>> GetActivesByUserIdsAsync(IReadOnlyList<long> userIds);
    }
}
