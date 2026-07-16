using Microsoft.Extensions.Logging;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Application.IServices;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Application.Storage;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Domain.Repositories;

namespace AcgFotos.Fotos.Application.Services;

/// <summary>
/// Paso de procesamiento de UNA foto (lo llama el worker, ya con el contexto del tenant armado):
/// lee el original, genera thumb + preview con watermark, los guarda y marca la foto Lista.
/// Cualquier fallo deja la foto en Error con detalle — el upload nunca "pierde" fotos en silencio,
/// el admin las ve en Error en su galería y puede re-subirlas.
/// </summary>
public class FotoProcesadorAppService : IFotoProcesadorAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFotoRepository _fotoRepository;
    private readonly IFotoStorage _fotoStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly OpcionesFotos _opciones;
    private readonly ILogger<FotoProcesadorAppService> _logger;

    public FotoProcesadorAppService(
        IUnitOfWork unitOfWork,
        IFotoRepository fotoRepository,
        IFotoStorage fotoStorage,
        IImageProcessor imageProcessor,
        OpcionesFotos opciones,
        ILogger<FotoProcesadorAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _fotoRepository = fotoRepository;
        _fotoStorage = fotoStorage;
        _imageProcessor = imageProcessor;
        _opciones = opciones;
        _logger = logger;
    }

    public async Task ProcesarAsync(long fotoId, CancellationToken cancellationToken = default)
    {
        var foto = await _fotoRepository.GetByIdTrackedAsync(fotoId);
        if (foto == null || foto.EstadoProcesamiento == EstadoProcesamientoFoto.Lista)
        {
            // Borrada entre el encole y el procesamiento, o re-encolada de más: nada que hacer.
            return;
        }

        try
        {
            var original = await _fotoStorage.LeerOriginalAsync(foto);
            using var stream = new MemoryStream(original);

            var derivados = await _imageProcessor.GenerarDerivadosAsync(
                stream,
                new OpcionesDerivados
                {
                    TextoWatermark = _opciones.TextoWatermark,
                    LadoMayorPreview = _opciones.LadoMayorPreview,
                    LadoMayorThumb = _opciones.LadoMayorThumb,
                    Calidad = _opciones.CalidadDerivados,
                },
                cancellationToken);

            await _fotoStorage.GuardarDerivadosAsync(foto, derivados);

            foto.Ancho = derivados.AnchoOriginal;
            foto.Alto = derivados.AltoOriginal;
            foto.EstadoProcesamiento = EstadoProcesamientoFoto.Lista;
            foto.ErrorProcesamiento = null;
        }
        catch (ImagenInvalidaException ex)
        {
            foto.EstadoProcesamiento = EstadoProcesamientoFoto.Error;
            foto.ErrorProcesamiento = ex.Message;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falló el procesamiento de la foto {FotoId}.", fotoId);
            foto.EstadoProcesamiento = EstadoProcesamientoFoto.Error;
            foto.ErrorProcesamiento = "Error inesperado al procesar la foto.";
        }

        await _unitOfWork.CommitAsync();
    }
}
