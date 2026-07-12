using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IUsuarioAppService : IExtendedEntityAppServiceBase<Usuario,
                                                                        UsuarioInputDto,
                                                                        UsuarioDto,
                                                                        UsuarioHeaderDto,
                                                                        ListaPaginadaCriteriaBase>
    {
        Task<UsuarioPerfilDto> GetPerfilAsync();

        Task<List<UsuarioDto>> GetUsuariosPorPermisosAsync(string[] permisos);

        Task ReConfirmarCuenta(long id);

        Task<bool> BloquearDesbloquear(string userName, bool state);

        Task<List<long>> GetUsuariosByRolIdAsync(long? rolId);

        Task<UsuarioDto> CreateTenantAdminUserAsync(long tenantId, UsuarioDto dto);

        Task<List<PermisoDto>> GetUsuarioPermisosAsync(string[] codigosPermiso);

        Task<UsuarioDto> UpdateProfileAsync(UsuarioPerfilDto dto);

        Task<List<UsuarioDatosPublicosDto>> GetUserDatosPublicosAsync();

        Task UpdateSecurityStampToTenantUsersAsync(long tenantId);

        Task<UsuarioHistorial> GetUltimoLoginAsync(long userId);

        /// <summary>
        /// Cambia el flag Administrador de un usuario. Requiere permiso de root/admin
        /// (validado en el Controller via [Permiso] + chequeo defensivo adentro).
        /// Endpoint separado del Update normal por defensa de mass assignment.
        /// </summary>
        Task SetAdministradorAsync(long id, bool administrador);

        /// <summary>
        /// Roles EFECTIVOS del usuario (directos ∪ de sus grupos) con su origen y nombres, para la
        /// visualización read-only en el ABM. No modifica nada.
        /// </summary>
        Task<List<RolEfectivoOutput>> GetRolesEfectivosAsync(long usuarioId);

        /// <summary>
        /// Serie mensual de actividad de usuarios del tenant para el gráfico del homepage: activos
        /// (logins distintos) y altas (creados) por mes, de los últimos <paramref name="meses"/> meses
        /// (default 12). Devuelve una entrada por mes, ordenada del más viejo al más nuevo y con ceros
        /// en los meses sin datos. Read-only.
        /// </summary>
        Task<List<UsuariosActividadMensualOutput>> GetActividadMensualAsync(int meses);
    }
}
