using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Controllers;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/auditoria")]
    public class AuditoriaController : ExtendedEntityApiControllerBase<Auditoria,
                                                                       AuditoriaDto,
                                                                       AuditoriaDto,
                                                                       AuditoriaHeaderDto,
                                                                       AuditoriaCriteria,
                                                                       IAuditoriaAppService> {

        public AuditoriaController(IAuditoriaAppService auditLogAppService) : base(auditLogAppService) {
        }

    }
}
