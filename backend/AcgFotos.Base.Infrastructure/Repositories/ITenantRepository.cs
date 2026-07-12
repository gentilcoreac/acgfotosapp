using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface ITenantRepository : IEntityBaseRepository<Tenant>
    {
        /// <summary>
        /// Listado paginado proyectado a TenantHeaderProjection. La proyección ocurre en SQL —
        /// no trae TenantLicenses/TenantAplicaciones/TenantFiles. El AppService mapea a TenantHeaderDto.
        /// </summary>
        Task<PaginationSet<TenantHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>Devuelve Tenant por Id sin tracking (read-only). Null si no existe.</summary>
        Task<Tenant> GetByIdReadOnlyAsync(long id);

        /// <summary>Tenant identificado por HostName o Codigo (case-insensitive). Read-only.</summary>
        Task<Tenant> GetByHostNameOrCodigoAsync(string value);

        /// <summary>Tenant con TenantLicenses + TenantAplicaciones (con Aplicacion). Read-only.</summary>
        Task<Tenant> GetByIdWithLicensesAndAplicacionesAsync(long id);

        /// <summary>Tenant con TenantAplicaciones, tracked (para edición posterior).</summary>
        Task<Tenant> GetByIdForUpdateAsync(long id);

        /// <summary>Todos los tenants excepto el dado. Read-only.</summary>
        Task<IReadOnlyList<Tenant>> GetTenantsExceptAsync(long tenantIdToExclude);

        /// <summary>Tenants sin custom theme (StyleSheetCssUrl vacío). Read-only.</summary>
        Task<IReadOnlyList<Tenant>> GetTenantsWithoutCustomThemeAsync();

        /// <summary>True si existe un tenant con el Nombre o Codigo dados.</summary>
        Task<bool> ExistsByNombreOrCodigoAsync(string nombre, string codigo);

        /// <summary>Listado paginado con TenantLicenses + TenantAplicaciones (con Aplicacion). Read-only.</summary>
        Task<PaginationSet<Tenant>> PaginateWithLicensesAndAplicacionesAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>Aplicaciones asociadas a un tenant (vía TenantAplicacion). Read-only.</summary>
        Task<IReadOnlyList<Aplicacion>> GetAplicacionesByTenantIdAsync(long tenantId);
    }
}
