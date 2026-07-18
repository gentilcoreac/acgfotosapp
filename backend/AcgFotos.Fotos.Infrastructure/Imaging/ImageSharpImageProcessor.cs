using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using AcgFotos.Fotos.Application.Imaging;

namespace AcgFotos.Fotos.Infrastructure.Imaging;

/// <summary>
/// Implementación ImageSharp del pipeline de derivados (ADR-01, capa 1): resize al lado mayor
/// pedido + marca de agua de texto repetida en diagonal sobre TODA la imagen (una marca en una
/// esquina se recorta fácil). Trabaja por copia en memoria: el stream original no se modifica.
/// </summary>
public class ImageSharpImageProcessor : IImageProcessor
{

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
            var anchoOriginal = imagen.Width;
            var altoOriginal = imagen.Height;

            var preview = await GenerarDerivadoAsync(imagen, opciones.LadoMayorPreview, opciones, cancellationToken);
            var thumb = await GenerarDerivadoAsync(imagen, opciones.LadoMayorThumb, opciones, cancellationToken);

            return new DerivadosFoto
            {
                Preview = preview,
                Thumb = thumb,
                AnchoOriginal = anchoOriginal,
                AltoOriginal = altoOriginal,
            };
        }
    }

    private static async Task<byte[]> GenerarDerivadoAsync(
        Image original, int ladoMayor, OpcionesDerivados opciones, CancellationToken cancellationToken)
    {
        // ResizeMode.Max encaja dentro de (ladoMayor x ladoMayor) conservando aspecto, pero también
        // AGRANDA una imagen menor — y agrandar un original chico no aporta nada (solo peso).
        var hayQueReducir = Math.Max(original.Width, original.Height) > ladoMayor;

        using var derivado = original.Clone(ctx =>
        {
            if (hayQueReducir)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(ladoMayor, ladoMayor),
                    Mode = ResizeMode.Max,
                });
            }
        });

        LimpiarMetadatos(derivado);
        AplicarWatermark(derivado, opciones.TextoWatermark, opciones.Opacidad);

        using var ms = new MemoryStream();
        // WebP lossy: ~25-30% menos peso que JPEG a igual calidad percibida (galería mobile con
        // datos móviles). Los derivados se regeneran, así que cambiar de formato es barato.
        await derivado.SaveAsync(ms, new WebpEncoder { Quality = opciones.Calidad }, cancellationToken);
        return ms.ToArray();
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

    private static void AplicarWatermark(Image imagen, string texto, float opacidad)
    {
        var font = ResolverFuente(imagen.Width);
        var color = Color.White.WithAlpha(opacidad);
        var medida = TextMeasurer.MeasureSize(texto, new TextOptions(font));

        // Grilla diagonal: el paso se deriva del tamaño del texto para que la densidad sea
        // parecida en el preview (900px) y en el thumb (300px). Apretada a pedido del negocio
        // (2026-07-16, "más invasiva pero sutil"): la sutileza la aporta el tamaño de letra
        // chico (ResolverFuente), no los huecos.
        var pasoX = medida.Width * 1.25f;
        var pasoY = medida.Height * 2.2f;

        imagen.Mutate(ctx =>
        {
            ctx.SetDrawingTransform(System.Numerics.Matrix3x2.CreateRotation(
                -0.4636f, // ~-26.5°, diagonal clásica de proofing
                new System.Numerics.Vector2(imagen.Width / 2f, imagen.Height / 2f)));

            // Se recorre un área mayor a la imagen para que la rotación no deje esquinas limpias.
            for (var y = (float)-imagen.Height; y < imagen.Height * 2f; y += pasoY)
            {
                // Alterna el corrimiento por fila (patrón ladrillo: más difícil de clonar/inpaint).
                var offsetX = (int)(y / pasoY) % 2 == 0 ? 0f : pasoX / 2f;
                for (var x = (float)-imagen.Width; x < imagen.Width * 2f; x += pasoX)
                {
                    ctx.DrawText(texto, font, color, new PointF(x + offsetX, y));
                }
            }
        });
    }

    private static Font ResolverFuente(int anchoImagen)
    {
        // El tamaño escala con la imagen (~1/20 del ancho): letra chica para que la frase completa
        // entre varias veces y el patrón cubra sin tapar la foto (más marcas, cada una más sutil).
        var tamano = Math.Max(11f, anchoImagen / 20f);

        // Arial está en Windows y en la mayoría de los Linux con fuentes MS; si no, cualquier fuente
        // del sistema sirve (el watermark no es tipográficamente exigente).
        if (SystemFonts.TryGet("Arial", out var arial))
        {
            return arial.CreateFont(tamano, FontStyle.Bold);
        }

        var familia = SystemFonts.Families.FirstOrDefault();
        if (familia == default)
        {
            throw new InvalidOperationException(
                "No hay fuentes del sistema disponibles para el watermark (¿contenedor sin fontconfig?).");
        }
        return familia.CreateFont(tamano, FontStyle.Bold);
    }
}
