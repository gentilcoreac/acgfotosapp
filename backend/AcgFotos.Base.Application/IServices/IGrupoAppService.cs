using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IGrupoAppService : IExtendedEntityAppServiceBase<Grupo,
                                                                      GrupoInputDto,
                                                                      GrupoDto,
                                                                      GrupoHeaderDto,
                                                                      ListaPaginadaCriteriaBase>
    {
    }
}
