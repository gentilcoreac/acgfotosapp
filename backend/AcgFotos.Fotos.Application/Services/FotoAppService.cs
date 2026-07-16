using Microsoft.Extensions.Logging;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Session;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Mappers.Mapperly;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Application.Storage;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// Upload masivo (Fase 1): guarda originales en storage privado, persiste las filas en Pendiente
/// y encola el procesamiento (thumb + preview con watermark) para no bloquear el request. No es un
/// CRUD Extended: la foto no se "edita" — se sube, se procesa y (más adelante) se borra.
/// </summary>
public class FotoAppService : IFotoAppService
{
    private const int MaxNombreArchivo = 255;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFotoRepository _fotoRepository;
    private readonly IFotoStorage _fotoStorage;
    private readonly FotoProcesamientoQueue _queue;
    private readonly IAppContext _appContext;
    private readonly FotoMapper _fotoMapper;
    private readonly ILogger<FotoAppService> _logger;

    public FotoAppService(
        IUnitOfWork unitOfWork,
        IFotoRepository fotoRepository,
        IFotoStorage fotoStorage,
        FotoProcesamientoQueue queue,
        IAppContext appContext,
        FotoMapper fotoMapper,
        ILogger<FotoAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _fotoRepository = fotoRepository;
        _fotoStorage = fotoStorage;
        _queue = queue;
        _appContext = appContext;
        _fotoMapper = fotoMapper;
        _logger = logger;
    }

    public async Task<List<FotoDto>> SubirAsync(SubirFotosInput input)
    {
        if (input.Archivos.Count == 0)
        {
            throw new BusinessValidationException("No se recibió ningún archivo.");
        }

        var vacio = input.Archivos.FirstOrDefault(a => a.Contenido.Length == 0);
        if (vacio != null)
        {
            throw new BusinessValidationException($"El archivo '{vacio.NombreArchivo}' está vacío.");
        }

        // El grupo se busca dentro del tenant (filtro global): un GrupoId ajeno da "no existe".
        var grupo = await _fotoRepository.GetGrupoAsync(input.GrupoId)
            ?? throw new BusinessValidationException("El grupo indicado no existe.");

        if (input.ParticipanteId is long participanteId
            && !await _fotoRepository.ParticipantePerteneceAlGrupoAsync(participanteId, grupo.Id))
        {
            throw new BusinessValidationException("El participante indicado no existe en el grupo.");
        }

        var fotos = input.Archivos.Select(archivo => new Foto
        {
            EventoId = grupo.EventoId,
            GrupoId = grupo.Id,
            ParticipanteId = input.ParticipanteId,
            StorageKey = Guid.NewGuid(),
            NombreArchivoOriginal = NormalizarNombre(archivo.NombreArchivo),
            TamanoBytes = archivo.Contenido.LongLength,
            EstadoProcesamiento = EstadoProcesamientoFoto.Pendiente,
            CreadoEn = DateTime.UtcNow,
        }).ToList();

        // Originales primero: si el storage falla no quedan filas; el huérfano inverso (archivo
        // sin fila si fallara el commit) no es visible ni peligroso, solo ocupa espacio.
        foreach (var (foto, archivo) in fotos.Zip(input.Archivos))
        {
            await _fotoStorage.GuardarOriginalAsync(foto, archivo.Contenido);
        }

        foreach (var foto in fotos)
        {
            _fotoRepository.Add(foto);
        }

        await _unitOfWork.CommitAsync();

        // Encolar DESPUÉS del commit: el worker debe encontrar la fila al procesarla.
        foreach (var foto in fotos)
        {
            _queue.Encolar(new FotoAProcesar(foto.Id, foto.TenantId, _appContext.UserId));
        }

        return fotos.Select(_fotoMapper.ToDto).ToList();
    }

    public async Task<List<FotoDto>> ListarAsync(long grupoId, long? participanteId)
    {
        var fotos = await _fotoRepository.ListarAsync(grupoId, participanteId);
        return fotos.Select(_fotoMapper.ToDto).ToList();
    }

    public async Task<byte[]?> ObtenerThumbAsync(long fotoId)
    {
        var foto = await _fotoRepository.GetByIdAsync(fotoId);
        return foto?.EstadoProcesamiento == EstadoProcesamientoFoto.Lista
            ? await _fotoStorage.LeerThumbAsync(foto)
            : null;
    }

    public async Task<byte[]?> ObtenerPreviewAsync(long fotoId)
    {
        var foto = await _fotoRepository.GetByIdAsync(fotoId);
        return foto?.EstadoProcesamiento == EstadoProcesamientoFoto.Lista
            ? await _fotoStorage.LeerPreviewAsync(foto)
            : null;
    }

    public async Task<DescargaOriginal?> ObtenerOriginalAsync(long fotoId)
    {
        var foto = await _fotoRepository.GetByIdAsync(fotoId);
        if (foto == null)
        {
            return null;
        }

        var contenido = await _fotoStorage.LeerOriginalAsync(foto);
        return new DescargaOriginal(contenido, foto.NombreArchivoOriginal);
    }

    public async Task<bool> EliminarAsync(long fotoId)
    {
        var foto = await _fotoRepository.GetByIdTrackedAsync(fotoId);
        if (foto == null)
        {
            return false;
        }

        // La fila primero (fuente de verdad de la galería); el storage después como best-effort:
        // un archivo huérfano no es visible ni peligroso, una fila sin archivos sí rompería.
        _fotoRepository.Delete(foto);
        await _unitOfWork.CommitAsync();

        try
        {
            await _fotoStorage.EliminarAsync(foto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron borrar los archivos de la foto {FotoId}; quedan huérfanos.", fotoId);
        }

        return true;
    }

    /// <summary>Solo el nombre (sin path del cliente), acotado al largo de la columna.</summary>
    private static string NormalizarNombre(string nombreArchivo)
    {
        var nombre = Path.GetFileName(nombreArchivo);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = "sin-nombre";
        }

        return nombre.Length <= MaxNombreArchivo ? nombre : nombre[^MaxNombreArchivo..];
    }
}
