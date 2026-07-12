using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Inputs;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/roles")]
    public class RolController : ExtendedEntityApiControllerBase<Rol,
                                                                 RolInputDto,
                                                                 RolDto,
                                                                 RolHeaderDto,
                                                                 ListaPaginadaCriteriaBase,
                                                                 IRolAppService>
    {
        public RolController(IRolAppService grupoAppService) : base(grupoAppService)
        {
        }

        [HttpGet]
        [Route("GetRolesByTipoLicencia")]
        public async Task<ActionResult<List<RolDto>>> GetRolesByTipoLicencia(long tipoLicenciaId)
        {
            var rta = await AppService.GetRolesByTipoLicenciaAsync(tipoLicenciaId);
            return this.Ok(rta);
        }

        [HttpGet]
        [Route("GetRolesPermisos")]
        public async Task<ActionResult<List<RolDto>>> GetRolesPermisos()
        {
            var rta = await AppService.GetRolesPermisosAsync();
            return this.Ok(rta);
        }

        /// <summary>
        /// Roles disponibles para el tenant del contexto según sus licencias contratadas
        /// (TenantLicencias → TipoLicencia → roles). Lo usa el ABM de grupos: el grupo solo puede
        /// otorgar roles que el tenant licenció (excluye huérfanos y roles de licencias ajenas).
        /// </summary>
        [HttpGet]
        [Route("del-tenant")]
        public async Task<ActionResult<List<RolDelTenantDto>>> GetRolesDelTenant()
        {
            var rta = await this.AppService.GetRolesDelTenantAsync();
            return this.Ok(rta);
        }

        /// <summary>
        /// Cambia el flag EsDefaultParaNuevoTenant de un rol. Endpoint dedicado, separado del
        /// Update normal: el RolInputDto NO expone el campo (defensa de mass assignment a nivel
        /// de tipo). Solo root (validación en AppService).
        /// </summary>
        [HttpPost]
        [Route("{id}/set-default-tenant")]
        public async Task<IActionResult> SetDefaultTenant(long id, [FromBody] SetDefaultTenantInput input)
        {
            await this.AppService.SetDefaultTenantAsync(id, input.EsDefaultParaNuevoTenant);
            return this.Ok();
        }
    }
}
