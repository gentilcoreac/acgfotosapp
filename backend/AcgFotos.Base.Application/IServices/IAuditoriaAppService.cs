using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IAuditoriaAppService : IExtendedEntityAppServiceBase<Auditoria,
                                                                          AuditoriaDto,
                                                                          AuditoriaDto,
                                                                          AuditoriaHeaderDto,
                                                                          AuditoriaCriteria> {
    }
}
