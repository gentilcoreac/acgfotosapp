using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Inputs;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/usuarios")]
    public class UsuarioController : ExtendedEntityApiControllerBase<Usuario,
                                                                     UsuarioInputDto,
                                                                     UsuarioDto,
                                                                     UsuarioHeaderDto,
                                                                     ListaPaginadaCriteriaBase,
                                                                     IUsuarioAppService>
    {
        private readonly ILicensesAppService _licensesAppService;

        public UsuarioController(IUsuarioAppService usuarioAppService, ILicensesAppService licensesAppService)
            : base(usuarioAppService)
        {
            _licensesAppService = licensesAppService;
        }

        /// <summary>
        /// Cambia el flag Administrador de un usuario. Endpoint dedicado, separado del Update normal: 
        /// el UsuarioInputDto NO expone Administrador para defender mass assignment a nivel de tipo. 
        /// Aquí se valida el permiso del caller (cinturón + tirantes — el AppService también lo gates en runtime).
        /// </summary>
        [HttpPost]
        [Route("{id}/set-administrador")]
        public async Task<IActionResult> SetAdministrador(long id, [FromBody] SetAdministradorInput input)
        {
            await this.AppService.SetAdministradorAsync(id, input.Administrador);
            return this.Ok();
        }

        /// <summary>
        /// Roles EFECTIVOS del usuario (directos ∪ de sus grupos) con su origen, para mostrarlos
        /// read-only en el ABM. La unión sale de la vista vw_UsuarioRolesEfectivos.
        /// </summary>
        [HttpGet]
        [Route("{id}/roles-efectivos")]
        public async Task<ActionResult<List<RolEfectivoOutput>>> GetRolesEfectivos(long id)
        {
            var roles = await this.AppService.GetRolesEfectivosAsync(id);
            return this.Ok(roles);
        }

        [HttpGet]
        [Route("mi-perfil")]
        public async Task<ActionResult<UsuarioPerfilDto>> GetPerfil()
        {
            var usuarioPerfilModel = await this.AppService.GetPerfilAsync();
            return this.Ok(usuarioPerfilModel);
        }

        [HttpPost]
        [Route("update-profile")]
        public async Task<ActionResult<UsuarioDto>> UpdateProfile(UsuarioPerfilDto dto)
        {
            var userDto = await this.AppService.UpdateProfileAsync(dto);
            return this.Ok(userDto);
        }

        [HttpPost]
        [Route("usuario-permisos")]
        public async Task<ActionResult<List<PermisoDto>>> GetUsuarioPermisos([FromBody] string[] permisos)
        {
            var usuarioPermisos = await this.AppService.GetUsuarioPermisosAsync(permisos);
            return this.Ok(usuarioPermisos);
        }

        [HttpGet]
        [Route("get-users-public-data")]
        public async Task<ActionResult<List<UsuarioDatosPublicosDto>>> GetUsuariosDatosPublicos()
        {
            var usuarios = await this.AppService.GetUserDatosPublicosAsync();
            return this.Ok(usuarios);
        }

        [HttpPost]
        [Route("re-confirmar-cuenta")]
        public async Task<IActionResult> ReConfirmarCuenta([FromBody] long usuarioId)
        {
            await this.AppService.ReConfirmarCuenta(usuarioId);
            return this.Ok(true);
        }

        [HttpPost]
        [Route("bloquear-desbloquear")]
        public async Task<IActionResult> BloquearDesbloquear([FromBody] UserStateInput userState)
        {
            var result = await this.AppService.BloquearDesbloquear(userState.UserName, userState.State);
            return this.Ok(result);
        }

        [HttpGet]
        [Route("usuarios-con-licencia")]
        public async Task<ActionResult<List<UsuarioDto>>> GetUsuariosConLicencia()
        {
            var usuarios = await _licensesAppService.GetUsuariosConLicenciaAsync();
            return this.Ok(usuarios);
        }

        [HttpGet]
        [Route("get-cantidad-licencias-asignadas")]
        public async Task<ActionResult<List<GetCantidadLicenciasDisponiblesOutput>>> GetCantidadLicenciasAsignadas()
        {
            var disponibles = await _licensesAppService.GetCantidadLicenciasDisponiblesAsync();
            return this.Ok(disponibles);
        }

        [HttpGet]
        [Route("actividad-mensual")]
        public async Task<ActionResult<List<UsuariosActividadMensualOutput>>> GetActividadMensual(int meses = 12)
        {
            var serie = await this.AppService.GetActividadMensualAsync(meses);
            return this.Ok(serie);
        }
    }
}
