using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface ILogInfoAppService : IEntityAppServiceBase<LogInfo, 
                                                                LogInfoDto, 
                                                                ListaPaginadaCriteriaBase>
    {
        /// <summary>
        /// Listado LIVIANO cross-tenant (root): NO trae los campos pesados (MessageTemplate, Exception,
        /// Properties). El registro completo se obtiene con <see cref="GetByIdForAllTenants"/>.
        /// </summary>
        PaginationSet<LogInfoAllOutput> GetForAllTenants(LogInfoCriteria criteria);

        /// <summary>Detalle COMPLETO de un log por id, cross-tenant (root). Trae Exception/Properties.</summary>
        LogInfoAllOutput GetByIdForAllTenants(long id);
    }
}
