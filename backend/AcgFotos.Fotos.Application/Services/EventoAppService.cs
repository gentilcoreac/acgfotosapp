using AutoMapper;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

public class EventoAppService : ExtendedEntityAppServiceBase<Evento,
                                                             EventoInputDto,
                                                             EventoDto,
                                                             EventoHeaderDto,
                                                             ListaPaginadaCriteriaBase>, IEventoAppService
{
    private readonly IEventoRepository _eventoRepository;
    private readonly EventoMapper _eventoMapper;

    public EventoAppService(
        IUnitOfWork unitOfWork,
        IEntityBaseRepository<Evento> entityRepository,
        IEventoRepository eventoRepository,
        IAppContext appContext,
        IMapper mapper,
        EventoMapper eventoMapper) : base(unitOfWork, entityRepository, appContext, mapper)
    {
        _eventoRepository = eventoRepository;
        _eventoMapper = eventoMapper;
    }

    public override async Task<PaginationSet<EventoHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
    {
        var page = await _eventoRepository.PaginateHeadersAsync(criteria);
        return page.MapItems(_eventoMapper.ToHeaderDto);
    }

    public override async Task<IEnumerable<EventoHeaderDto>> GetAllAsync()
    {
        var entities = await this.EntityRepository.GetAllAsync();
        return entities.Select(_eventoMapper.ToHeaderDto);
    }

    public override async Task<EventoDto?> GetByIdAsync(long id)
    {
        var entity = await _eventoRepository.GetByIdWithTamanosReadOnlyAsync(id);
        return entity == null ? null : _eventoMapper.ToDto(entity);
    }

    protected override async Task<Evento> GetEntityToUpdateAsync(long id) =>
        (await _eventoRepository.GetByIdWithTamanosAsync(id))!;

    protected override EventoDto ToOutput(Evento entity) => _eventoMapper.ToDto(entity);

    // Los tamaños llegan como filas completas: se reconcilia por Id (0 = alta, ausente = baja,
    // presente = se actualizan sus campos). No sirve ChildCollectionSync.SyncBy (solo claves):
    // acá las filas existentes cambian Nombre/Precio/Orden/Activo.
    protected override void SyncCollections(Evento evento, EventoInputDto dto)
    {
        var deseados = dto.TamanosPrecios;
        var idsDeseados = deseados.Where(t => t.Id != 0).Select(t => t.Id).ToHashSet();

        foreach (var quitado in evento.TamanosPrecios.Where(t => !idsDeseados.Contains(t.Id)).ToList())
        {
            evento.TamanosPrecios.Remove(quitado);
        }

        var porId = evento.TamanosPrecios.ToDictionary(t => t.Id);
        foreach (var fila in deseados)
        {
            if (fila.Id != 0 && porId.TryGetValue(fila.Id, out var existente))
            {
                existente.Nombre = fila.Nombre;
                existente.PrecioUnitario = fila.PrecioUnitario;
                existente.Orden = fila.Orden;
                existente.Activo = fila.Activo;
            }
            else if (fila.Id == 0)
            {
                evento.TamanosPrecios.Add(new TamanoPrecio
                {
                    Nombre = fila.Nombre,
                    PrecioUnitario = fila.PrecioUnitario,
                    Orden = fila.Orden,
                    Activo = fila.Activo,
                });
            }
        }
    }
}
