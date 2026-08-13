using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Infrastructure.Imaging;

/// <summary>
/// Implementación del pipeline de derivados (ADR-01, capa 1): resize al lado mayor pedido +
/// composición de capas de marca de agua (ADR-15) sobre TODA la imagen. Decode/resize/EXIF/encode
/// van por ImageSharp; la composición de capas va por SkiaSharp (ADR-16 — ImageSharp no tiene todos
/// los modos de fusión). Trabaja por copia en memoria: el stream original no se modifica.
/// </summary>
public class ImageSharpImageProcessor : IImageProcessor
{
    // El código anterior a ADR-15/16 dibujaba el texto vectorial directo a la resolución final del
    // derivado (sin paso de transformación de por medio). Ahora la capa ya nace rasterizada (front) y
    // el backend la rota/escala — sin pedir explícitamente filtro+antialiasing, SkiaSharp deja bordes
    // dentados en la rotación y aliasing en el escalado hacia abajo.
    private static readonly SKSamplingOptions SamplingCalidad = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    public async Task<DerivadosFoto> GenerarDerivadosAsync(
        Stream original,
        OpcionesDerivados opciones,
        CancellationToken cancellationToken = default)
    {
        Image imagen;
        try
        {
            imagen = await Image.LoadAsync(original, cancellationToken);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new ImagenInvalidaException("El archivo no es una imagen válida.", ex);
        }

        using (imagen)
        {
            // Una foto vertical suele venir con los píxeles acostados más una marca EXIF que dice cómo
            // rotarla. Como LimpiarMetadatos borra ese EXIF (GPS y datos del equipo no viajan a las
            // familias), hay que aplicar la rotación a los píxeles ACÁ: si no, se pierde la única
            // indicación de orientación y las verticales quedan acostadas para siempre.
            imagen.Mutate(ctx => ctx.AutoOrient());

            var anchoOriginal = imagen.Width;
            var altoOriginal = imagen.Height;

            var preview = GenerarDerivado(imagen, opciones.LadoMayorPreview, opciones, marcar: true);
            var thumb = GenerarDerivado(imagen, opciones.LadoMayorThumb, opciones, marcar: opciones.MarcarThumb);

            return new DerivadosFoto
            {
                Preview = preview,
                Thumb = thumb,
                AnchoOriginal = anchoOriginal,
                AltoOriginal = altoOriginal,
            };
        }
    }

    private static byte[] GenerarDerivado(Image original, int ladoMayor, OpcionesDerivados opciones, bool marcar)
    {
        // ResizeMode.Max encaja dentro de (ladoMayor x ladoMayor) conservando aspecto, pero también
        // AGRANDA una imagen menor — y agrandar un original chico no aporta nada (solo peso).
        var hayQueReducir = Math.Max(original.Width, original.Height) > ladoMayor;

        using var derivado = original.CloneAs<Rgba32>();
        if (hayQueReducir)
        {
            derivado.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(ladoMayor, ladoMayor),
                Mode = ResizeMode.Max,
            }));
        }

        LimpiarMetadatos(derivado);

        // WebP lossy: ~25-30% menos peso que JPEG a igual calidad percibida (galería mobile con
        // datos móviles). Los derivados se regeneran, así que cambiar de formato es barato.
        if (!marcar || opciones.Capas.Count == 0)
        {
            using var sinMarca = new MemoryStream();
            derivado.SaveAsWebp(sinMarca, new WebpEncoder { Quality = opciones.Calidad });
            return sinMarca.ToArray();
        }

        // Dos pasadas, y este es el punto de todo: comprimir la foto ANTES de sellarla. Si se
        // compusiera la marca y después se comprimiera todo junto, la compresión con pérdida se
        // comería justo lo que la marca tiene (bordes finos, texto chico) y la autoría del fotógrafo
        // saldría borrosa. Acá la foto llega al sellado ya degradada de forma irreversible —no
        // recupera nada— y lo único que la segunda pasada preserva es la marca.
        return ComponerYCodificar(SoloFotoComprimida(derivado, opciones.Calidad), opciones);
    }

    /// <summary>Deja la foto con el daño de compresión ya hecho, para que el sellado no lo herede.</summary>
    private static Image<Rgba32> SoloFotoComprimida(Image<Rgba32> derivado, int calidad)
    {
        using var ms = new MemoryStream();
        derivado.SaveAsWebp(ms, new WebpEncoder { Quality = calidad });
        ms.Position = 0;
        return Image.Load<Rgba32>(ms);
    }

    private static byte[] ComponerYCodificar(Image<Rgba32> fotoYaDegradada, OpcionesDerivados opciones)
    {
        using (fotoYaDegradada)
        {
            LimpiarMetadatos(fotoYaDegradada);
            ComponerCapas(fotoYaDegradada, opciones.Capas);

            using var ms = new MemoryStream();
            fotoYaDegradada.SaveAsWebp(ms, new WebpEncoder { Quality = opciones.CalidadMarcado });
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Son fotos de menores (docs/05-notas-abiertas.md): el original que sube el fotógrafo puede traer
    /// GPS del lugar del evento y datos del equipo en el EXIF. El original no se toca (útil para
    /// imprimir), pero ese EXIF no tiene por qué viajar en los derivados que ven las familias — el
    /// watermark es la única marca que debe sobrevivir a una descarga/reenvío.
    /// </summary>
    private static void LimpiarMetadatos(Image imagen)
    {
        imagen.Metadata.ExifProfile = null;
        imagen.Metadata.IptcProfile = null;
        imagen.Metadata.XmpProfile = null;
    }

    /// <summary>
    /// Puente ImageSharp ↔ SkiaSharp (ADR-16): copia los píxeles del derivado a un <see cref="SKBitmap"/>,
    /// compone las capas en orden con <see cref="SKCanvas"/> (coloca, escala sólo hacia abajo, rota,
    /// funde con <c>SKBlendMode</c>), y copia el resultado de vuelta. La API nunca dibuja texto — cada
    /// capa ya es un PNG rasterizado por el front (o el asset por defecto embebido, ver
    /// <c>ConfiguracionFotosResolver</c>).
    /// </summary>
    private static void ComponerCapas(Image<Rgba32> derivado, IReadOnlyList<CapaComposicion> capas)
    {
        var ancho = derivado.Width;
        var alto = derivado.Height;
        var pixelBytes = new byte[ancho * alto * 4];
        derivado.CopyPixelDataTo(pixelBytes);

        using var bitmap = new SKBitmap(new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        var handle = GCHandle.Alloc(pixelBytes, GCHandleType.Pinned);
        try
        {
            bitmap.InstallPixels(bitmap.Info, handle.AddrOfPinnedObject(), bitmap.Info.RowBytes);

            using (var canvas = new SKCanvas(bitmap))
            {
                foreach (var capa in capas.OrderBy(c => c.Orden))
                {
                    ComponerCapa(canvas, ancho, alto, capa);
                }
            }

            derivado.ProcessPixelRows(accessor => CopiarBitmapAImagen(bitmap, accessor));
        }
        finally
        {
            handle.Free();
        }
    }

    private static void ComponerCapa(SKCanvas canvas, int anchoFoto, int altoFoto, CapaComposicion capa)
    {
        using var bitmap = SKBitmap.Decode(capa.Asset);
        if (bitmap is null)
        {
            return; // el asset se valida al subirlo (grupo 4); un fallo acá no debe tumbar el procesamiento.
        }

        // SKImage en vez de dibujar el SKBitmap crudo: Skia cachea los niveles de mipmap contra la
        // imagen, así que decodificar y envolver una sola vez por capa (no por cada tile del modo
        // Repetida) evita recalcularlos en cada uno de los N draws.
        using var asset = SKImage.FromBitmap(bitmap);

        // Escala en % del ancho de la foto, pero NUNCA hacia arriba (ADR-15 §8): agrandar un bitmap
        // no tiene arreglo, así que el piso es el tamaño natural del asset.
        var anchoDestino = Math.Min(anchoFoto * (capa.EscalaPorcentaje / 100f), asset.Width);
        var altoDestino = asset.Height * (anchoDestino / asset.Width);

        using var paint = new SKPaint
        {
            BlendMode = MapearModoFusion(capa.ModoFusion),
            Color = new SKColor(255, 255, 255, (byte)Math.Clamp(capa.Opacidad * 255f, 0, 255)),
            IsAntialias = true,
        };

        if (capa.ModoColocacion == ModoColocacionMarcaAgua.PosicionFija)
        {
            var margen = anchoFoto * (capa.MargenPorcentaje / 100f);
            var (x, y) = CalcularPosicionFija(capa.Posicion ?? PosicionMarcaAgua.Centro,
                anchoFoto, altoFoto, anchoDestino, altoDestino, margen);

            canvas.Save();
            canvas.RotateDegrees(capa.AnguloGrados, x + anchoDestino / 2f, y + altoDestino / 2f);
            canvas.DrawImage(asset, SKRect.Create(x, y, anchoDestino, altoDestino), SamplingCalidad, paint);
            canvas.Restore();
            return;
        }

        // Repetida: grilla en mosaico con offset alternado por fila (patrón ladrillo), rotada como
        // conjunto — se recorre un área mayor a la foto para que la rotación no deje esquinas limpias.
        // El paso horizontal sale de la separación pedida sobre la FOTO, no del tamaño del tile: por eso
        // achicar la marca ya no multiplica las repeticiones. El vertical se deriva del horizontal
        // conservando la proporción del patrón, para que la grilla no cambie de forma al variar la
        // separación.
        // Una separación no positiva dejaría la grilla sin avanzar y la foto SIN MARCA: se cae al paso
        // histórico (1.25× el ancho del tile) en vez de no dibujar nada — quedarse sin watermark en
        // silencio es la falla que ADR-01 no puede permitir.
        var separacion = capa.SeparacionPorcentaje > 0f
            ? anchoFoto * (capa.SeparacionPorcentaje / 100f)
            : anchoDestino * 1.25f;

        var pitchX = separacion;
        var pitchY = altoDestino * (pitchX / anchoDestino) * CapaMarcaAgua.FactorPasoVertical;
        if (pitchX <= 0 || pitchY <= 0)
        {
            return;
        }

        canvas.Save();
        canvas.RotateDegrees(capa.AnguloGrados, anchoFoto / 2f, altoFoto / 2f);

        var fila = 0;
        for (var y = (float)-altoFoto; y < altoFoto * 2f; y += pitchY)
        {
            var offsetX = fila % 2 == 0 ? 0f : pitchX / 2f;
            for (var x = -anchoFoto + offsetX; x < anchoFoto * 2f; x += pitchX)
            {
                canvas.DrawImage(asset, SKRect.Create(x, y, anchoDestino, altoDestino), SamplingCalidad, paint);
            }
            fila++;
        }

        canvas.Restore();
    }

    private static (float X, float Y) CalcularPosicionFija(
        PosicionMarcaAgua posicion, int anchoFoto, int altoFoto, float anchoCapa, float altoCapa, float margen)
    {
        var x = posicion switch
        {
            PosicionMarcaAgua.ArribaIzquierda or PosicionMarcaAgua.CentroIzquierda or PosicionMarcaAgua.AbajoIzquierda
                => margen,
            PosicionMarcaAgua.ArribaCentro or PosicionMarcaAgua.Centro or PosicionMarcaAgua.AbajoCentro
                => (anchoFoto - anchoCapa) / 2f,
            _ => anchoFoto - anchoCapa - margen,
        };

        var y = posicion switch
        {
            PosicionMarcaAgua.ArribaIzquierda or PosicionMarcaAgua.ArribaCentro or PosicionMarcaAgua.ArribaDerecha
                => margen,
            PosicionMarcaAgua.CentroIzquierda or PosicionMarcaAgua.Centro or PosicionMarcaAgua.CentroDerecha
                => (altoFoto - altoCapa) / 2f,
            _ => altoFoto - altoCapa - margen,
        };

        return (x, y);
    }

    private static SKBlendMode MapearModoFusion(ModoFusionMarcaAgua modo) => modo switch
    {
        ModoFusionMarcaAgua.Normal => SKBlendMode.SrcOver,
        ModoFusionMarcaAgua.Superponer => SKBlendMode.Overlay,
        ModoFusionMarcaAgua.Diferencia => SKBlendMode.Difference,
        _ => throw new ArgumentOutOfRangeException(nameof(modo), modo, null),
    };

    private static void CopiarBitmapAImagen(SKBitmap bitmap, PixelAccessor<Rgba32> accessor)
    {
        for (var y = 0; y < accessor.Height; y++)
        {
            var fila = accessor.GetRowSpan(y);
            for (var x = 0; x < fila.Length; x++)
            {
                var color = bitmap.GetPixel(x, y);
                fila[x] = new Rgba32(color.Red, color.Green, color.Blue, color.Alpha);
            }
        }
    }
}
