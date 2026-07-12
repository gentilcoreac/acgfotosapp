using AcgFotos.Core.Application;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.IServices;

public interface IEventoAppService : IExtendedEntityAppServiceBase<Evento,
                                                                   EventoInputDto,
                                                                   EventoDto,
                                                                   EventoHeaderDto,
                                                                   ListaPaginadaCriteriaBase>
{
}
