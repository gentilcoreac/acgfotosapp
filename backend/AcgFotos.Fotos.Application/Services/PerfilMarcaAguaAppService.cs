using AutoMapper;
using Microsoft.Extensions.Logging;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Application.Security;
using AcgFotos.Fotos.Application.Storage;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// CRUD de perfiles de marca de agua (ADR-15). El alta y la subida de contenido de una capa van por
/// <see cref="SubirCapaAsync"/> (design.md D14: la primera capa crea el perfil); el CRUD estándar
/// sólo edita metadata y la colocación de capas ya existentes — <see cref="PerfilMarcaAguaInputDto"/>
/// rechaza filas de capa con Id 0.
/// </summary>
public class PerfilMarcaAguaAppService : ExtendedEntityAppServiceBase<PerfilMarcaAgua,
                                                                      PerfilMarcaAguaInputDto,
                                                                      PerfilMarcaAguaDto,
                                                                      PerfilMarcaAguaDto,
                                                                      ListaPaginadaCriteriaBase>, IPerfilMarcaAguaAppService
{
    private const int MaxCapas = 3;
    private const string NombrePerfilPorDefecto = "Nuevo perfil";

    // Valores iniciales de colocación de una capa recién subida: mosaico (el modo "más difícil de
    // recortar", ver ModoColocacionMarcaAgua.Repetida) discreto — el fotógrafo los ajusta en el editor.
    private const float EscalaPorcentajeDefault = 20f;
    private const float MargenPorcentajeDefault = 5f;
    private const float OpacidadDefault = 1f;

    private readonly IPerfilMarcaAguaRepository _perfilRepository;
    private readonly IValidadorAssetMarcaAgua _validadorAsset;
    private readonly IFotoStorage _fotoStorage;
    private readonly PerfilMarcaAguaMapper _perfilMapper;
    private readonly ILogger<PerfilMarcaAguaAppService> _logger;

    public PerfilMarcaAguaAppService(
        IUnitOfWork unitOfWork,
        IEntityBaseRepository<PerfilMarcaAgua> entityRepository,
        IPerfilMarcaAguaRepository perfilRepository,
        IValidadorAssetMarcaAgua validadorAsset,
        IFotoStorage fotoStorage,
        IAppContext appContext,
        IMapper mapper,
        PerfilMarcaAguaMapper perfilMapper,
        ILogger<PerfilMarcaAguaAppService> logger) : base(unitOfWork, entityRepository, appContext, mapper)
    {
        _perfilRepository = perfilRepository;
        _validadorAsset = validadorAsset;
        _fotoStorage = fotoStorage;
        _perfilMapper = perfilMapper;
        _logger = logger;
    }

    // Base.SearchAsync/GetAllAsync mapean por AutoMapper (sin perfil registrado para esta entidad,
    // y no traen las capas); acá se resuelve por Mapperly + el repo con Include.
    public override async Task<PaginationSet<PerfilMarcaAguaDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        await this.EnsurePerfilEstandarSembradoAsync();
        var items = (await _perfilRepository.GetAllConCapasReadOnlyAsync()).Select(ToOutput).ToList();

        // Sin repo de paginación dedicado: son pocos perfiles por tenant (ADR-15 §1), alcanza en memoria.
        return new PaginationSet<PerfilMarcaAguaDto>
        {
            Page = 0,
            TotalCount = items.Count,
            TotalPages = 1,
            Items = items,
        };
    }

    public override async Task<IEnumerable<PerfilMarcaAguaDto>> GetAllAsync()
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        await this.EnsurePerfilEstandarSembradoAsync();
        return (await _perfilRepository.GetAllConCapasReadOnlyAsync()).Select(ToOutput);
    }

    public override async Task<PerfilMarcaAguaDto?> GetByIdAsync(long id)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        var entity = await _perfilRepository.GetByIdConCapasReadOnlyAsync(id);
        return entity == null ? null : ToOutput(entity);
    }

    protected override async Task<PerfilMarcaAgua> GetEntityToUpdateAsync(long id) =>
        (await _perfilRepository.GetByIdConCapasAsync(id))!;

    protected override PerfilMarcaAguaDto ToOutput(PerfilMarcaAgua entity)
    {
        var dto = _perfilMapper.ToDto(entity);
        dto.Avisos = ConstruirAvisos(entity);
        return dto;
    }

    // Antes de guardar: si pasa a default desmarca el anterior (mismo commit); si algún capa deja de
    // estar en el dto, se borra su asset del storage DESPUÉS de confirmar el commit (best-effort,
    // mismo criterio que FotoAppService.EliminarAsync).
    public override async Task<PerfilMarcaAguaDto> UpdateAsync(PerfilMarcaAguaInputDto dto)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        this.CheckInputValidations(dto); // primero la forma (la base la repite; es idempotente)

        if (dto.EsDefault)
        {
            await this.LimpiarDefaultAnteriorAsync(dto.Id);
        }

        var storageKeysARemover = new List<Guid>();
        if (dto.Id != 0)
        {
            var actual = await _perfilRepository.GetByIdConCapasReadOnlyAsync(dto.Id);
            var idsDeseados = dto.Capas.Select(c => c.Id).ToHashSet();
            storageKeysARemover = actual?.Capas
                .Where(c => !idsDeseados.Contains(c.Id))
                .Select(c => c.StorageKey)
                .ToList() ?? new List<Guid>();
        }

        var resultado = await base.UpdateAsync(dto);

        foreach (var storageKey in storageKeysARemover)
        {
            try
            {
                await _fotoStorage.EliminarCapaMarcaAguaAsync(resultado.Id, storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo borrar el asset de una capa quitada del perfil {PerfilId}; queda huérfano.",
                    resultado.Id);
            }
        }

        return resultado;
    }

    // Sólo reconcilia colocación/orden y bajas de capas YA EXISTENTES (Id > 0, garantizado por el
    // validador): el alta de una capa nueva siempre pasa por SubirCapaAsync (D14).
    protected override void SyncCollections(PerfilMarcaAgua perfil, PerfilMarcaAguaInputDto dto)
    {
        var idsDeseados = dto.Capas.Select(c => c.Id).ToHashSet();

        foreach (var quitada in perfil.Capas.Where(c => !idsDeseados.Contains(c.Id)).ToList())
        {
            perfil.Capas.Remove(quitada);
        }

        var porId = perfil.Capas.ToDictionary(c => c.Id);
        foreach (var fila in dto.Capas)
        {
            if (!porId.TryGetValue(fila.Id, out var existente))
            {
                throw new BusinessValidationException(
                    "La capa indicada no pertenece a este perfil: subí su imagen antes de guardarla acá.");
            }

            existente.Orden = fila.Orden;
            existente.ModoColocacion = fila.ModoColocacion;
            existente.Posicion = fila.Posicion;
            existente.EscalaPorcentaje = fila.EscalaPorcentaje;
            existente.MargenPorcentaje = fila.MargenPorcentaje;
            existente.AnguloGrados = fila.AnguloGrados;
            existente.Opacidad = fila.Opacidad;
            existente.ModoFusion = fila.ModoFusion;
        }
    }

    public override async Task DeleteByIdAsync(long id)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        var perfil = await _perfilRepository.GetByIdConCapasReadOnlyAsync(id);

        await base.DeleteByIdAsync(id); // cascade borra fot_CapasMarcaAgua (CapaMarcaAguaConfig)

        if (perfil == null)
        {
            return;
        }

        foreach (var capa in perfil.Capas)
        {
            try
            {
                await _fotoStorage.EliminarCapaMarcaAguaAsync(perfil.Id, capa.StorageKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudieron borrar los assets del perfil {PerfilId}; quedan huérfanos.", perfil.Id);
            }
        }
    }

    public async Task<CapaMarcaAguaSubidaDto> SubirCapaAsync(SubirCapaMarcaAguaInput input)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);

        var resultado = await _validadorAsset.ValidarAsync(input.Contenido);

        PerfilMarcaAgua perfil;
        if (input.PerfilMarcaAguaId is long perfilId)
        {
            perfil = await _perfilRepository.GetByIdConCapasAsync(perfilId)
                ?? throw new BusinessValidationException("El perfil indicado no existe.");
        }
        else
        {
            perfil = new PerfilMarcaAgua
            {
                Nombre = string.IsNullOrWhiteSpace(input.NombrePerfilSiNuevo)
                    ? NombrePerfilPorDefecto
                    : input.NombrePerfilSiNuevo!,
                MarcarThumb = true,
            };
            this.EntityRepository.Add(perfil);
        }

        if (perfil.Capas.Count >= MaxCapas)
        {
            throw new BusinessValidationException($"El perfil ya tiene el máximo de {MaxCapas} capas.");
        }

        var capa = new CapaMarcaAgua
        {
            StorageKey = Guid.NewGuid(),
            Orden = perfil.Capas.Count == 0 ? 0 : perfil.Capas.Max(c => c.Orden) + 1,
            AnchoPx = resultado.AnchoPx,
            AltoPx = resultado.AltoPx,
            ModoColocacion = ModoColocacionMarcaAgua.Repetida,
            EscalaPorcentaje = EscalaPorcentajeDefault,
            MargenPorcentaje = MargenPorcentajeDefault,
            AnguloGrados = 0f,
            Opacidad = OpacidadDefault,
            ModoFusion = ModoFusionMarcaAgua.Normal,
        };
        perfil.Capas.Add(capa);

        // Recién ahora perfil.Id (y el de la capa) están poblados: la key de storage los necesita.
        await this.UnitOfWork.CommitAsync();
        await _fotoStorage.GuardarCapaMarcaAguaAsync(perfil.Id, capa.StorageKey, input.Contenido);

        var avisos = new List<string>();
        if (!resultado.TieneCanalAlfa)
        {
            avisos.Add("La imagen no tiene transparencia: se va a componer con su fondo sólido.");
        }

        var evaluacionEscala = _validadorAsset.EvaluarEscala(resultado.AnchoPx, capa.EscalaPorcentaje);
        if (!evaluacionEscala.Alcanza)
        {
            avisos.Add(
                $"La imagen tiene {resultado.AnchoPx}px de ancho; a esta escala hacen falta al menos " +
                $"{evaluacionEscala.AnchoNecesarioPx}px — se va a ver borrosa.");
        }

        return new CapaMarcaAguaSubidaDto
        {
            Perfil = this.ToOutput(perfil),
            Capa = _perfilMapper.ToDto(capa),
            Avisos = avisos,
        };
    }

    public async Task<byte[]?> ObtenerAssetCapaAsync(long perfilId, Guid storageKey)
    {
        FamiliaSessionGuard.EnsureNoFamiliaSession(this.AppContext);
        var perfil = await _perfilRepository.GetByIdConCapasReadOnlyAsync(perfilId);
        var existe = perfil?.Capas.Any(c => c.StorageKey == storageKey) ?? false;
        return existe ? await _fotoStorage.LeerCapaMarcaAguaAsync(perfilId, storageKey) : null;
    }

    // Seed del perfil "Estándar" (ADR-15 §1, design.md D11), tomada al implementar 5.9: no hay
    // ningún mecanismo de seed en C# en el repo hoy, y este es un dato de negocio por TENANT (no de
    // instalación) — se siembra la primera vez que el tenant abre el listado, reusando los dos
    // métodos reales (SubirCapaAsync + UpdateAsync) en vez de escribir filas/storage a mano. Nunca
    // se marca default (D11): sin él, la cascada cae en OpcionesFotos igual — existe para que el
    // fotógrafo lo vea y entienda el mecanismo, no para cambiar el comportamiento real.
    private const string NombrePerfilEstandar = "Estándar";

    private async Task EnsurePerfilEstandarSembradoAsync()
    {
        var yaExiste = (await this.EntityRepository.GetAllAsync()).Any(p => p.Nombre == NombrePerfilEstandar);
        if (yaExiste)
        {
            return;
        }

        try
        {
            var assetDefault = await _fotoStorage.LeerCapaMarcaAguaDefaultAsync();
            var subida = await this.SubirCapaAsync(new SubirCapaMarcaAguaInput
            {
                NombrePerfilSiNuevo = NombrePerfilEstandar,
                Contenido = assetDefault,
            });

            // SubirCapaAsync sólo da valores genéricos de arranque; acá se ajusta la colocación para
            // reproducir el aspecto de hoy (mismos parámetros que el fallback embebido, D13).
            await this.UpdateAsync(new PerfilMarcaAguaInputDto
            {
                Id = subida.Perfil.Id,
                Nombre = subida.Perfil.Nombre,
                EsDefault = false,
                MarcarThumb = subida.Perfil.MarcarThumb,
                Capas = new List<CapaMarcaAguaDto>
                {
                    new()
                    {
                        Id = subida.Capa.Id,
                        Orden = subida.Capa.Orden,
                        ModoColocacion = ModoColocacionMarcaAgua.Repetida,
                        EscalaPorcentaje = MarcaAguaLegadoConstantes.EscalaPorcentaje,
                        MargenPorcentaje = subida.Capa.MargenPorcentaje,
                        AnguloGrados = MarcaAguaLegadoConstantes.AnguloGrados,
                        Opacidad = MarcaAguaLegadoConstantes.Opacidad,
                        ModoFusion = ModoFusionMarcaAgua.Normal,
                    },
                },
            });
        }
        catch (Exception ex)
        {
            // No debe romper el listado: el perfil "Estándar" es una comodidad de demo (D11), el
            // pipeline real sigue funcionando por el fallback embebido si esto falla.
            _logger.LogError(ex,
                "No se pudo sembrar el perfil de marca de agua \"Estándar\" (tenant {TenantId}).",
                this.AppContext.TenantId);
        }
    }

    private async Task LimpiarDefaultAnteriorAsync(long idActual)
    {
        var actuales = await this.EntityRepository.GetAllAsync();
        foreach (var otro in actuales.Where(p => p.EsDefault && p.Id != idActual))
        {
            otro.EsDefault = false;
        }
    }

    private static List<string> ConstruirAvisos(PerfilMarcaAgua entity)
    {
        var avisos = new List<string>();
        if (entity.Capas.Count > 0 && entity.Capas.All(c => c.Opacidad <= 0f))
        {
            avisos.Add("Las familias van a ver estas fotos sin ninguna protección: ninguna capa queda visible.");
        }

        return avisos;
    }
}
