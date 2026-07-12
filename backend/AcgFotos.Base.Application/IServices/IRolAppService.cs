using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IRolAppService : IExtendedEntityAppServiceBase<Rol,
                                                                    RolInputDto,
                                                                    RolDto,
                                                                    RolHeaderDto,
                                                                    ListaPaginadaCriteriaBase>
    {
        Task<List<Rol>> GetRolesByUsuarioIdAsync(long id);

        Task<PaginationSet<RolDto>> SearchByUserAplicacionesAsync(ListaPaginadaCriteriaBase criteria);

        Task<List<Rol>> GetRolesByTipoLicenciaAsync(long tipoLicenciaId);

        /// <summary>
        /// Roles disponibles para el tenant del contexto según sus licencias contratadas, cada uno con
        /// la(s) licencia(s) del tenant que lo incluyen (chips, §11.1). Lo usa el ABM de grupos: el grupo
        /// solo puede otorgar roles que el tenant licenció.
        /// </summary>
        Task<List<RolDelTenantDto>> GetRolesDelTenantAsync();

        Task<List<Rol>> GetRolesPermisosAsync();

        /// <summary>
        /// Cambia el flag EsDefaultParaNuevoTenant de un rol. Requiere root.
        /// Endpoint dedicado por defensa de mass assignment.
        /// </summary>
        Task SetDefaultTenantAsync(long id, bool esDefaultParaNuevoTenant);
    }
}
