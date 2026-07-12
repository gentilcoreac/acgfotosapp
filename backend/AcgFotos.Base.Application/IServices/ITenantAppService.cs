using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface ITenantAppService : IExtendedEntityAppServiceBase<Tenant,
                                                                       TenantInputDto,
                                                                       TenantDto,
                                                                       TenantHeaderDto,
                                                                       ListaPaginadaCriteriaBase>
    {
        Task<TenantHeaderStyleDto> EditByAdminClientAsync(TenantEditByAdminClientInput tenantInput);

        Task<List<TenantHeaderDto>> GetTenantsForUserRootAsync();

        Task<TenantHeaderStyleDto> GetTenantStyleByIdAsync(long id);

        Task<GetTenantPublicStyleOutput> GetTenantPublicStyleAsync(string valueToFilter);

        Task RegenerarTodosLosThemesAsync();

        Task RegenerarThemePorTenantIdAsync(long tenantId);

        Task<bool> IsActiveAsync(long tenantId);

        /// <summary>
        /// Administradores del tenant (solo lectura, para mostrarlos en el edit del ABM). Endpoint
        /// dedicado: NO se mete en <c>GetByIdAsync</c> para no cargar la query cross-tenant en el
        /// detalle estándar. Nunca viaja en el add/edit.
        /// </summary>
        Task<List<TenantAdminUsuarioDto>> GetAdministradoresAsync(long tenantId);
    }
}
