using AutoMapper;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Application.Security;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// CRUD de conjuntos de opciones de publicación (ADR-15 §5): eje independiente del perfil de marca,
/// mismo patrón "a lo sumo un default por tenant" sostenido acá, no por un índice filtrado
/// (<c>OpcionesPublicacionConfig</c>).
/// </summary>
public class OpcionesPublicacionAppService : EntityAppServiceBase<OpcionesPublicacion,
                                                                  OpcionesPublicacionDto,
                                                                  ListaPaginadaCriteriaBase>, IOpcionesPublicacionAppService
{
    private readonly IOpcionesPublicacionRepository _opcionesRepository;
    private readonly OpcionesPublicacionMapper _opcionesMapper;

    public OpcionesPublicacionAppService(
        IUnitOfWork unitOfWork,
        IEntityBaseRepository<OpcionesPublicacion> entityRepository,
        IOpcionesPublicacionRepository opcionesRepository,
        IAppContext appContext,
        IMapper mapper,
        OpcionesPublicacionMapper opcionesMapper) : base(unitOfWork, entityRepository, appContext, mapper)
    {
        _opcionesRepository = opcionesRepository;
        _opcionesMapper = opcionesMapper;
    }

    // Base.SearchAsync/GetAllAsync mapean por AutoMapper (sin perfil registrado para esta entidad);
    // acá se resuelve por Mapperly, mismo criterio que ParametroAppService/EventoAppService.
    public override async Task<PaginationSet<OpcionesPublicacionDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        var page = await this.EntityRepository.PaginateAsync(criteria);
        return page.MapItems(_opcionesMapper.ToDto);
    }

    public override async Task<IEnumerable<OpcionesPublicacionDto>> GetAllAsync()
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        var entidades = await this.EntityRepository.GetAllAsync();
        return entidades.Select(_opcionesMapper.ToDto);
    }

    public override async Task<OpcionesPublicacionDto?> GetByIdAsync(long id)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        var entity = await this.EntityRepository.GetByIdAsync(id);
        return entity == null ? null : _opcionesMapper.ToDto(entity);
    }

    // Antes de guardar: si este conjunto pasa a default, desmarca el anterior (mismo ChangeTracker,
    // un solo commit — ver EventoAppService/GrupoAppService para el mismo criterio de pre-check async).
    public override async Task<OpcionesPublicacionDto> UpdateAsync(OpcionesPublicacionDto dto)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        this.CheckInputValidations(dto);

        if (dto.EsDefault)
        {
            var actuales = await this.EntityRepository.GetAllAsync();
            foreach (var otro in actuales.Where(o => o.EsDefault && o.Id != dto.Id))
            {
                otro.EsDefault = false;
            }
        }

        return await base.UpdateAsync(dto);
    }

    public override async Task DeleteByIdAsync(long id)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        await base.DeleteByIdAsync(id);
    }

    protected override OpcionesPublicacionDto ToOutput(OpcionesPublicacion entity) => _opcionesMapper.ToDto(entity);
}
