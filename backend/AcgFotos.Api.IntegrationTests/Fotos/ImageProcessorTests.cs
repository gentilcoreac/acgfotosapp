using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Infrastructure.Imaging;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Humo del pipeline de derivados (ADR-01 capa 1). Tests puros: sin host ni DB.
    /// La imagen de prueba se genera en memoria (un rectángulo de color plano) para que la
    /// verificación del watermark sea determinística: cualquier variación de píxeles en el
    /// derivado solo puede venir de la marca de agua.
    /// </summary>
    public class ImageProcessorTests
    {
        private static readonly ImageSharpImageProcessor Processor = new();

        private static MemoryStream CrearJpegPlano(int ancho, int alto)
        {
            using var imagen = new Image<Rgb24>(ancho, alto, new Rgb24(40, 90, 160));
            var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            ms.Position = 0;
            return ms;
        }

        private static OpcionesDerivados Opciones() => new() { TextoWatermark = "ACG Fotos" };

        [Fact]
        public async Task Genera_preview_y_thumb_con_los_lados_mayores_pedidos()
        {
            using var original = CrearJpegPlano(4000, 3000);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            Assert.Equal(4000, derivados.AnchoOriginal);
            Assert.Equal(3000, derivados.AltoOriginal);

            using var preview = Image.Load(derivados.PreviewJpeg);
            Assert.Equal(1200, Math.Max(preview.Width, preview.Height));
            // Conserva el aspecto 4:3.
            Assert.Equal(900, Math.Min(preview.Width, preview.Height));

            using var thumb = Image.Load(derivados.ThumbJpeg);
            Assert.Equal(300, Math.Max(thumb.Width, thumb.Height));
        }

        [Fact]
        public async Task No_agranda_una_imagen_menor_al_lado_pedido()
        {
            using var original = CrearJpegPlano(800, 600);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            using var preview = Image.Load(derivados.PreviewJpeg);
            Assert.Equal(800, preview.Width);
            Assert.Equal(600, preview.Height);
        }

        [Fact]
        public async Task El_watermark_altera_pixeles_en_toda_la_imagen()
        {
            using var original = CrearJpegPlano(2000, 1500);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            // Sobre un color plano, los píxeles "más claros que el fondo" solo pueden ser watermark
            // (blanco al 35%). Se exige presencia en los CUATRO cuadrantes: una marca solo en el
            // centro o en una esquina se recorta fácil y no cumple ADR-01.
            using var preview = Image.Load<Rgb24>(derivados.PreviewJpeg);
            var cuadrantesConMarca = new bool[2, 2];
            preview.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var fila = accessor.GetRowSpan(y);
                    for (var x = 0; x < fila.Length; x++)
                    {
                        // Umbral holgado sobre el canal rojo (fondo=40 + watermark blanco lo sube).
                        if (fila[x].R > 90)
                        {
                            cuadrantesConMarca[y * 2 / accessor.Height, x * 2 / fila.Length] = true;
                        }
                    }
                }
            });

            Assert.True(cuadrantesConMarca[0, 0], "sin watermark en el cuadrante superior izquierdo");
            Assert.True(cuadrantesConMarca[0, 1], "sin watermark en el cuadrante superior derecho");
            Assert.True(cuadrantesConMarca[1, 0], "sin watermark en el cuadrante inferior izquierdo");
            Assert.True(cuadrantesConMarca[1, 1], "sin watermark en el cuadrante inferior derecho");
        }

        [Fact]
        public async Task Contenido_no_imagen_tira_ImagenInvalidaException()
        {
            using var noImagen = new MemoryStream("esto no es un jpg"u8.ToArray());

            await Assert.ThrowsAsync<ImagenInvalidaException>(
                () => Processor.GenerarDerivadosAsync(noImagen, Opciones()));
        }
    }
}
