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

    public FotoAppService(
        IUnitOfWork unitOfWork,
        IFotoRepository fotoRepository,
        IFotoStorage fotoStorage,
        FotoProcesamientoQueue queue,
        IAppContext appContext,
        FotoMapper fotoMapper)
    {
        _unitOfWork = unitOfWork;
        _fotoRepository = fotoRepository;
        _fotoStorage = fotoStorage;
        _queue = queue;
        _appContext = appContext;
        _fotoMapper = fotoMapper;
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

        // El curso se busca dentro del tenant (filtro global): un CursoId ajeno da "no existe".
        var curso = await _fotoRepository.GetCursoAsync(input.CursoId)
            ?? throw new BusinessValidationException("El curso indicado no existe.");

        if (input.AlbumId is long albumId
            && !await _fotoRepository.AlbumPerteneceAlCursoAsync(albumId, curso.Id))
        {
            throw new BusinessValidationException("El álbum indicado no existe en el curso.");
        }

        var fotos = input.Archivos.Select(archivo => new Foto
        {
            EventoId = curso.EventoId,
            CursoId = curso.Id,
            AlbumId = input.AlbumId,
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

    public async Task<List<FotoDto>> ListarAsync(long cursoId, long? albumId)
    {
        var fotos = await _fotoRepository.ListarAsync(cursoId, albumId);
        return fotos.Select(_fotoMapper.ToDto).ToList();
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
