using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface ITipoLicenciaAppService : IExtendedEntityAppServiceBase<TipoLicencia,
                                                                             TipoLicenciaInputDto,
                                                                             TipoLicenciaDto,
                                                                             TipoLicenciaHeaderDto,
                                                                             ListaPaginadaCriteriaBase>
    {
        /// <summary>
        /// Cambia el flag EsDefaultParaNuevoTenant de un tipo de licencia. Requiere root.
        /// Endpoint dedicado por defensa de mass assignment.
        /// </summary>
        Task SetDefaultTenantAsync(long id, bool esDefaultParaNuevoTenant);
    }
}
