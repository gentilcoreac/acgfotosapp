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

public class CursoAppService : ExtendedEntityAppServiceBase<Curso,
                                                            CursoInputDto,
                                                            CursoDto,
                                                            CursoHeaderDto,
                                                            CursoCriteria>, ICursoAppService
{
    private readonly ICursoRepository _cursoRepository;
    private readonly CursoMapper _cursoMapper;
    private readonly IGeneradorQr _generadorQr;
    private readonly OpcionesFotos _opciones;

    public CursoAppService(
        IUnitOfWork unitOfWork,
        IEntityBaseRepository<Curso> entityRepository,
        ICursoRepository cursoRepository,
        IAppContext appContext,
        IMapper mapper,
        CursoMapper cursoMapper,
        IGeneradorQr generadorQr,
        OpcionesFotos opciones) : base(unitOfWork, entityRepository, appContext, mapper)
    {
        _cursoRepository = cursoRepository;
        _cursoMapper = cursoMapper;
        _generadorQr = generadorQr;
        _opciones = opciones;
    }

    public async Task<TarjetasCursoDto?> GetTarjetasAsync(long cursoId)
    {
        var curso = await _cursoRepository.GetByIdParaTarjetasAsync(cursoId);
        if (curso == null)
        {
            return null;
        }

        var tarjetas = curso.Albumes.OrderBy(a => a.NombreAlumno).Select(album =>
        {
            // El mapper ya resuelve cuál es el código vigente del álbum.
            var codigo = _cursoMapper.ToDto(album).CodigoAcceso;
            var url = codigo == null ? null : _opciones.UrlCanjeTemplate.Replace("{codigo}", codigo);

            return new TarjetaAlbumDto
            {
                AlbumId = album.Id,
                NombreAlumno = album.NombreAlumno,
                Codigo = codigo,
                UrlCanje = url,
                QrPngBase64 = url == null ? null : Convert.ToBase64String(_generadorQr.GenerarPng(url)),
            };
        }).ToList();

        return new TarjetasCursoDto
        {
            CursoId = curso.Id,
            NombreCurso = curso.Nombre,
            NombreEvento = curso.Evento.Nombre,
            Tarjetas = tarjetas,
        };
    }

    public override async Task<PaginationSet<CursoHeaderDto>> SearchAsync(CursoCriteria criteria)
    {
        var page = await _cursoRepository.PaginateHeadersAsync(criteria, criteria.EventoId);
        return page.MapItems(_cursoMapper.ToHeaderDto);
    }

    public override async Task<IEnumerable<CursoHeaderDto>> GetAllAsync()
    {
        var entities = await _cursoRepository.GetAllWithAlbumesReadOnlyAsync();
        return entities.Select(_cursoMapper.ToHeaderDto);
    }

    public override async Task<CursoDto?> GetByIdAsync(long id)
    {
        var entity = await _cursoRepository.GetByIdWithAlbumesReadOnlyAsync(id);
        return entity == null ? null : _cursoMapper.ToDto(entity);
    }

    protected override async Task<Curso> GetEntityToUpdateAsync(long id) =>
        (await _cursoRepository.GetByIdWithAlbumesAsync(id))!;

    protected override CursoDto ToOutput(Curso entity) => _cursoMapper.ToDto(entity);

    // Guards con consulta async que los hooks sync de la base no permiten. El del evento es de
    // seguridad multi-tenant: EventoId viene del input y la FK sola aceptaría un evento de OTRO
    // tenant (el filtro global no valida FKs). El de fotos evita que la FK Restrict reviente en
    // el commit con un 500: acá se corta antes con un 400 explicable.
    public override async Task<CursoDto> UpdateAsync(CursoInputDto dto)
    {
        this.CheckInputValidations(dto); // primero la forma (la base la repite; es idempotente)

        if (!await _cursoRepository.ExisteEventoAsync(dto.EventoId))
        {
            throw new BusinessValidationException("El evento indicado no existe.");
        }

        if (dto.Id != 0)
        {
            var idsDeseados = dto.Albumes.Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();
            var conFotos = await _cursoRepository.GetAlbumIdsConFotosAsync(dto.Id);
            if (conFotos.Any(id => !idsDeseados.Contains(id)))
            {
                throw new BusinessValidationException(
                    "No se puede eliminar un álbum que tiene fotos: primero hay que borrar sus fotos.");
            }
        }

        return await base.UpdateAsync(dto);
    }

    public override async Task DeleteByIdAsync(long id)
    {
        if (await _cursoRepository.TieneFotosAsync(id))
        {
            throw new BusinessValidationException(
                "No se puede eliminar un curso que tiene fotos: primero hay que borrar sus fotos.");
        }

        await base.DeleteByIdAsync(id);
    }

    // Los álbumes llegan como filas completas y se reconcilian por Id (0 = alta, ausente = baja,
    // presente = update del nombre). Al alta se le genera su código de acceso: el álbum nace
    // canjeable (la tarjeta para la familia sale de acá). El código nunca se pisa en ediciones.
    protected override void SyncCollections(Curso curso, CursoInputDto dto)
    {
        var idsDeseados = dto.Albumes.Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();

        foreach (var quitado in curso.Albumes.Where(a => !idsDeseados.Contains(a.Id)).ToList())
        {
            curso.Albumes.Remove(quitado);
        }

        var porId = curso.Albumes.ToDictionary(a => a.Id);
        foreach (var fila in dto.Albumes)
        {
            if (fila.Id != 0 && porId.TryGetValue(fila.Id, out var existente))
            {
                existente.NombreAlumno = fila.NombreAlumno;
            }
            else if (fila.Id == 0)
            {
                curso.Albumes.Add(new Album
                {
                    NombreAlumno = fila.NombreAlumno,
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
