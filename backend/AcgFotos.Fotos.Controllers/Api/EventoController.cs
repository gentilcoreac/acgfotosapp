using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.Application;
using AcgFotos.Core.Controllers;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Controllers.Api
{
    [ApiController]
    [Route("api/fotos/eventos")]
    public class EventoController : ExtendedEntityApiControllerBase<Evento,
                                                                    EventoInputDto,
                                                                    EventoDto,
                                                                    EventoHeaderDto,
                                                                    ListaPaginadaCriteriaBase,
                                                                    IEventoAppService>
    {
        public EventoController(IEventoAppService eventoAppService) : base(eventoAppService)
        {
        }
    }
}
