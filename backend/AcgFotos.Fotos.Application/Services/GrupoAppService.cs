using AutoMapper;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Criterias;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Application.Tarjetas;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;
using AcgFotos.Fotos.Domain.Services;

namespace AcgFotos.Fotos.Application.Services;

public class GrupoAppService : ExtendedEntityAppServiceBase<Grupo,
                                                            GrupoInputDto,
                                                            GrupoDto,
                                                            GrupoHeaderDto,
                                                            GrupoCriteria>, IGrupoAppService
{
    private readonly IGrupoRepository _grupoRepository;
    private readonly GrupoMapper _grupoMapper;
    private readonly IGeneradorQr _generadorQr;
    private readonly OpcionesFotos _opciones;

    public GrupoAppService(
        IUnitOfWork unitOfWork,
        IEntityBaseRepository<Grupo> entityRepository,
        IGrupoRepository grupoRepository,
        IAppContext appContext,
        IMapper mapper,
        GrupoMapper grupoMapper,
        IGeneradorQr generadorQr,
        OpcionesFotos opciones) : base(unitOfWork, entityRepository, appContext, mapper)
    {
        _grupoRepository = grupoRepository;
        _grupoMapper = grupoMapper;
        _generadorQr = generadorQr;
        _opciones = opciones;
    }

    public async Task<TarjetasGrupoDto?> GetTarjetasAsync(long grupoId)
    {
        var grupo = await _grupoRepository.GetByIdParaTarjetasAsync(grupoId);
        if (grupo == null)
        {
            return null;
        }

        var tarjetas = grupo.Participantes.OrderBy(a => a.Nombre).Select(participante =>
        {
            // El mapper ya resuelve cuál es el código vigente del participante.
            var codigo = _grupoMapper.ToDto(participante).CodigoAcceso;
            var url = codigo == null ? null : _opciones.UrlCanjeTemplate.Replace("{codigo}", codigo);

            return new TarjetaParticipanteDto
            {
                ParticipanteId = participante.Id,
                Nombre = participante.Nombre,
                Codigo = codigo,
                UrlCanje = url,
                QrPngBase64 = url == null ? null : Convert.ToBase64String(_generadorQr.GenerarPng(url)),
            };
        }).ToList();

        return new TarjetasGrupoDto
        {
            GrupoId = grupo.Id,
            NombreGrupo = grupo.Nombre,
            NombreEvento = grupo.Evento.Nombre,
            Tarjetas = tarjetas,
        };
    }

    public override async Task<PaginationSet<GrupoHeaderDto>> SearchAsync(GrupoCriteria criteria)
    {
        var page = await _grupoRepository.PaginateHeadersAsync(criteria, criteria.EventoId);
        return page.MapItems(_grupoMapper.ToHeaderDto);
    }

    public override async Task<IEnumerable<GrupoHeaderDto>> GetAllAsync()
    {
        var entities = await _grupoRepository.GetAllWithParticipantesReadOnlyAsync();
        return entities.Select(_grupoMapper.ToHeaderDto);
    }

    public override async Task<GrupoDto?> GetByIdAsync(long id)
    {
        var entity = await _grupoRepository.GetByIdWithParticipantesReadOnlyAsync(id);
        return entity == null ? null : _grupoMapper.ToDto(entity);
    }

    protected override async Task<Grupo> GetEntityToUpdateAsync(long id) =>
        (await _grupoRepository.GetByIdWithParticipantesAsync(id))!;

    protected override GrupoDto ToOutput(Grupo entity) => _grupoMapper.ToDto(entity);

    // Guards con consulta async que los hooks sync de la base no permiten. El del evento es de
    // seguridad multi-tenant: EventoId viene del input y la FK sola aceptaría un evento de OTRO
    // tenant (el filtro global no valida FKs). El de fotos evita que la FK Restrict reviente en
    // el commit con un 500: acá se corta antes con un 400 explicable.
    public override async Task<GrupoDto> UpdateAsync(GrupoInputDto dto)
    {
        this.CheckInputValidations(dto); // primero la forma (la base la repite; es idempotente)

        if (!await _grupoRepository.ExisteEventoAsync(dto.EventoId))
        {
            throw new BusinessValidationException("El evento indicado no existe.");
        }

        if (dto.Id != 0)
        {
            var idsDeseados = dto.Participantes.Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();
            var conFotos = await _grupoRepository.GetParticipanteIdsConFotosAsync(dto.Id);
            if (conFotos.Any(id => !idsDeseados.Contains(id)))
            {
                throw new BusinessValidationException(
                    "No se puede eliminar un participante que tiene fotos: primero hay que borrar sus fotos.");
            }
        }

        return await base.UpdateAsync(dto);
    }

    public override async Task DeleteByIdAsync(long id)
    {
        if (await _grupoRepository.TieneFotosAsync(id))
        {
            throw new BusinessValidationException(
                "No se puede eliminar un grupo que tiene fotos: primero hay que borrar sus fotos.");
        }

        await base.DeleteByIdAsync(id);
    }

    // Los participantes llegan como filas completas y se reconcilian por Id (0 = alta, ausente = baja,
    // presente = update del nombre). Al alta se le genera su código de acceso: el participante nace
    // canjeable (la tarjeta para la familia sale de acá). El código nunca se pisa en ediciones.
    protected override void SyncCollections(Grupo grupo, GrupoInputDto dto)
    {
        var idsDeseados = dto.Participantes.Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();

        foreach (var quitado in grupo.Participantes.Where(a => !idsDeseados.Contains(a.Id)).ToList())
        {
            grupo.Participantes.Remove(quitado);
        }

        var porId = grupo.Participantes.ToDictionary(a => a.Id);
        foreach (var fila in dto.Participantes)
        {
            if (fila.Id != 0 && porId.TryGetValue(fila.Id, out var existente))
            {
                existente.Nombre = fila.Nombre;
            }
            else if (fila.Id == 0)
            {
                grupo.Participantes.Add(new Participante
                {
                    Nombre = fila.Nombre,
                    CodigosAcceso =
                    {
                        new CodigoAcceso
                        {
                            Codigo = GeneradorCodigoAcceso.Generar(),
                            Activo = true,
                            CreadoEn = DateTime.UtcNow,
                        },
                    },
                });
            }
        }
    }
}
