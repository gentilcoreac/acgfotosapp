using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using AcgFotos.Core.Exceptions;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Application.Procesamiento;

namespace AcgFotos.Fotos.Infrastructure.Imaging;

public class ValidadorAssetMarcaAgua : IValidadorAssetMarcaAgua
{
    private readonly OpcionesFotos _opciones;

    public ValidadorAssetMarcaAgua(OpcionesFotos opciones)
    {
        _opciones = opciones;
    }

    public async Task<ResultadoValidacionAsset> ValidarAsync(byte[] contenido, CancellationToken cancellationToken = default)
    {
        if (contenido.Length > _opciones.PesoMaximoAssetMarcaAguaBytes)
        {
            throw new BusinessValidationException(
                $"El archivo pesa {contenido.Length / 1024} KB; el máximo admitido es " +
                $"{_opciones.PesoMaximoAssetMarcaAguaBytes / 1024} KB.");
        }

        // Identifica dimensiones leyendo sólo la cabecera, SIN decodificar el bitmap completo —
        // ataja una bomba de descompresión (un archivo chico que declara dimensiones enormes) antes
        // de que el decode intente reservar memoria por ellas.
        ImageInfo info;
        try
        {
            using var streamIdentify = new MemoryStream(contenido);
            var identificada = await Image.IdentifyAsync(streamIdentify, cancellationToken);
            info = identificada ?? throw new BusinessValidationException("El archivo no es una imagen válida.");
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new BusinessValidationException("El archivo no es una imagen válida.");
        }

        if (info.Width > _opciones.AnchoMaximoAssetMarcaAguaPx || info.Height > _opciones.AltoMaximoAssetMarcaAguaPx)
        {
            throw new BusinessValidationException(
                $"La imagen mide {info.Width}×{info.Height}px; el máximo admitido es " +
                $"{_opciones.AnchoMaximoAssetMarcaAguaPx}×{_opciones.AltoMaximoAssetMarcaAguaPx}px.");
        }

        // Recién acá se decodifica completo: ya se descartaron los archivos que ni siquiera valía la
        // pena intentar decodificar. El formato real lo determina el decoder, no la extensión ni el
        // content-type declarado por el cliente.
        Image imagen;
        try
        {
            using var streamDecode = new MemoryStream(contenido);
            imagen = await Image.LoadAsync(streamDecode, cancellationToken);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new BusinessValidationException("El archivo no es una imagen válida.");
        }

        using (imagen)
        {
            var metadataPng = imagen.Metadata.GetPngMetadata();
            var esPng = imagen.Metadata.DecodedImageFormat is PngFormat;
            if (!esPng)
            {
                throw new BusinessValidationException(
                    "El archivo debe ser un PNG con transparencia (ADR-15) — se detectó otro formato al decodificarlo.");
            }

            var tieneCanalAlfa = metadataPng.ColorType is PngColorType.RgbWithAlpha or PngColorType.GrayscaleWithAlpha;

            return new ResultadoValidacionAsset
            {
                AnchoPx = imagen.Width,
                AltoPx = imagen.Height,
                TieneCanalAlfa = tieneCanalAlfa,
            };
        }
    }

    public EvaluacionEscala EvaluarEscala(int anchoAssetPx, float escalaPorcentaje, int anchoReferenciaFotoPx = 1600)
    {
        var anchoNecesario = (int)(anchoReferenciaFotoPx * (escalaPorcentaje / 100f));
        return new EvaluacionEscala
        {
            AnchoNecesarioPx = anchoNecesario,
            Alcanza = anchoAssetPx >= anchoNecesario,
        };
    }
}
