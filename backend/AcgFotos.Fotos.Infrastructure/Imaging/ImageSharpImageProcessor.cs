using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
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
    // La opacidad es un equilibrio: suficiente para arruinar una impresión, sin impedir elegir la foto.
    private const float OpacidadWatermark = 0.35f;

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
                PreviewJpeg = preview,
                ThumbJpeg = thumb,
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

        AplicarWatermark(derivado, opciones.TextoWatermark);

        using var ms = new MemoryStream();
        await derivado.SaveAsync(ms, new JpegEncoder { Quality = opciones.CalidadJpeg }, cancellationToken);
        return ms.ToArray();
    }

    private static void AplicarWatermark(Image imagen, string texto)
    {
        var font = ResolverFuente(imagen.Width);
        var color = Color.White.WithAlpha(OpacidadWatermark);
        var medida = TextMeasurer.MeasureSize(texto, new TextOptions(font));

        // Grilla diagonal: el paso se deriva del tamaño del texto para que la densidad sea
        // parecida en el preview (1200px) y en el thumb (300px).
        var pasoX = medida.Width * 1.6f;
        var pasoY = medida.Height * 5f;

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
        // El tamaño escala con la imagen (~1/12 del ancho) para cubrirla igual en thumb y preview.
        var tamano = Math.Max(12f, anchoImagen / 12f);

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
