using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Application.Outputs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AcgFotos.Base.Controllers.Api
{

    [ApiController]
    [Route("api/general/tenants")]
    public class TenantController : ExtendedEntityApiControllerBase<Tenant,
                                                                    TenantInputDto,
                                                                    TenantDto,
                                                                    TenantHeaderDto,
                                                                    ListaPaginadaCriteriaBase,
                                                                    ITenantAppService>
    {

        public TenantController(ITenantAppService tenantAppService) : base(tenantAppService)
        {

        }

        [HttpGet]
        [Route("header-style/{id}")]
        public async Task<ActionResult<TenantHeaderStyleDto>> GetTenantHeaderById(long id)
        {
            var tenant = await this.AppService.GetTenantStyleByIdAsync(id);
            if (tenant != null)
            {
                return this.Ok(tenant);
            }
            else
            {
                return this.NotFound();
            }
        }

        [HttpGet]
        [Route("tenants-for-root")]
        public async Task<ActionResult<List<TenantHeaderDto>>> GetTenantsForUserRoot()
        {
            var clientes = await this.AppService.GetTenantsForUserRootAsync();
            return this.Ok(clientes);
        }

        // Administradores del tenant (solo lectura, para el edit del ABM). Dedicado: NO va en el
        // detalle (GET {id}) para no cargar la query cross-tenant en cada lectura del tenant.
        [HttpGet("{id:int}/administradores")]
        public async Task<ActionResult<List<TenantAdminUsuarioDto>>> GetAdministradores(long id)
        {
            var admins = await this.AppService.GetAdministradoresAsync(id);
            return this.Ok(admins);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("public-style/{valueToFilter}")]
        public async Task<ActionResult<GetTenantPublicStyleOutput>> GetTenantPublicStyle(string valueToFilter)
        {
            var tenant = await this.AppService.GetTenantPublicStyleAsync(valueToFilter);
            return this.Ok(tenant);
        }

        // Estos dos endpoints triggerean operaciones de I/O (escritura de archivos CSS).
        // Antes estaban [AllowAnonymous]: cualquiera podía forzar regeneración masiva (DoS).
        // Ahora requieren autenticación. La validación fina (root-only) se hace en el AppService.
        [HttpGet("regenerar-theme-por-tenant-id/{tenantId}")]
        public async Task<IActionResult> RegenerarThemePorTenantId(long tenantId)
        {
            await this.AppService.RegenerarThemePorTenantIdAsync(tenantId);
            return this.Ok();
        }

        [HttpGet("regenerar-todos-los-themes")]
        public async Task<IActionResult> RegenerarTodosLosThemes()
        {
            await this.AppService.RegenerarTodosLosThemesAsync();
            return this.Ok();
        }

        [HttpPost]
        [Route("edit-by-admin-client")]
        public async Task<ActionResult<TenantHeaderStyleDto>> EditByAdminClient(TenantEditByAdminClientInput dto)
        {
            var result = await this.AppService.EditByAdminClientAsync(dto);
            return this.Ok(result);
        }
    }
}
