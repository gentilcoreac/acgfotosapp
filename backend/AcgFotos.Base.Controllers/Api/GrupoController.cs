using Microsoft.AspNetCore.Mvc;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Base.Controllers.Api
{
    [ApiController]
    [Route("api/general/grupos")]
    public class GrupoController : ExtendedEntityApiControllerBase<Grupo,
                                                                   GrupoInputDto,
                                                                   GrupoDto,
                                                                   GrupoHeaderDto,
                                                                   ListaPaginadaCriteriaBase,
                                                                   IGrupoAppService>
    {
        public GrupoController(IGrupoAppService grupoAppService) : base(grupoAppService)
        {
        }
    }
}
