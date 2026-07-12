using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Inputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IParametroValorTenantAppService : IEntityAppServiceBase<ParametroValorTenant,
                                                                             ParametroValorTenantDto,
                                                                             ListaPaginadaCriteriaBase>
    {
        Task ValorizarParametroAsync(ParametroValorValorizarInput input);
    }
}
