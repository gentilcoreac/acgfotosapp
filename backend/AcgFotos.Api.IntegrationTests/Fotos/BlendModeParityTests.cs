using System.Reflection;
using SkiaSharp;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// ADR-16 (docs/04-decisiones.md) / design D7 (openspec/changes/marca-agua-configurable): la
    /// composición de capas usa SkiaSharp (no ImageSharp — su <c>PixelColorBlendingMode</c> no tiene
    /// <c>Difference</c>, ver ADR-16) precisamente porque <c>SKBlendMode</c> y el
    /// <c>globalCompositeOperation</c> de canvas implementan la misma especificación W3C — se
    /// verifica UNA vez con esta muestra, no en cada build; nadie edita nunca una fórmula de fusión.
    ///
    /// Los fixtures embebidos (<c>Fotos/Fixtures/blend-canvas-*.png</c>) se generaron con
    /// Playwright/Chromium sobre EXACTAMENTE esta misma base: 4 cuadrantes de 8×8 sin antialiasing
    /// (blanco/negro/gris medio/rojo saturado — cubre "clara/oscura/mixta") con una capa blanca al
    /// 50% encima, el caso real que motivó ADR-15 §3 (el blanco al 50% que desaparece sobre un
    /// vestido claro). El script generador no se versiona (se corrió una sola vez); esta clase deja
    /// la base 100% reproducible por si hiciera falta regenerarlos.
    /// </summary>
    public class BlendModeParityTests
    {
        private const int TamanoCuadrante = 8;
        private const int Lado = TamanoCuadrante * 2;

        // Tolerancia por canal: absorbe redondeo entre la composición de SkiaSharp y el pipeline de
        // 8 bits del encoder/decoder PNG del navegador — no una discrepancia real de fórmula (eso
        // haría fallar el test igual, la tolerancia es angosta).
        private const int ToleranciaPorCanal = 2;

        private static SKBitmap CrearBaseConCapa(SKBlendMode modo)
        {
            var bitmap = new SKBitmap(Lado, Lado, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bitmap);

            using (var paint = new SKPaint { Color = new SKColor(255, 255, 255) })
            {
                canvas.DrawRect(SKRect.Create(0, 0, TamanoCuadrante, TamanoCuadrante), paint); // clara
                paint.Color = new SKColor(0, 0, 0);
                canvas.DrawRect(SKRect.Create(TamanoCuadrante, 0, TamanoCuadrante, TamanoCuadrante), paint); // oscura
                paint.Color = new SKColor(128, 128, 128);
                canvas.DrawRect(SKRect.Create(0, TamanoCuadrante, TamanoCuadrante, TamanoCuadrante), paint); // media
                paint.Color = new SKColor(200, 50, 50);
                canvas.DrawRect(SKRect.Create(TamanoCuadrante, TamanoCuadrante, TamanoCuadrante, TamanoCuadrante), paint); // saturada
            }

            // La capa: blanco al 50%, el caso real de ADR-15 §3.
            using var capa = new SKPaint { Color = new SKColor(255, 255, 255, 128), BlendMode = modo };
            canvas.DrawRect(SKRect.Create(0, 0, Lado, Lado), capa);

            return bitmap;
        }

        private static SKBitmap CargarFixture(string nombreArchivo)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                .Single(n => n.EndsWith(nombreArchivo, StringComparison.OrdinalIgnoreCase));
            using var stream = assembly.GetManifestResourceStream(name)!;
            return SKBitmap.Decode(stream);
        }

        [Theory]
        [InlineData(SKBlendMode.SrcOver, "blend-canvas-normal.png")]
        [InlineData(SKBlendMode.Overlay, "blend-canvas-overlay.png")]
        [InlineData(SKBlendMode.Difference, "blend-canvas-difference.png")]
        public void SkiaSharp_compone_igual_que_canvas(SKBlendMode modo, string archivoFixture)
        {
            using var real = CrearBaseConCapa(modo);
            using var esperado = CargarFixture(archivoFixture);

            Assert.Equal(esperado.Width, real.Width);
            Assert.Equal(esperado.Height, real.Height);

            for (var y = 0; y < Lado; y++)
            {
                for (var x = 0; x < Lado; x++)
                {
                    var pixelReal = real.GetPixel(x, y);
                    var pixelCanvas = esperado.GetPixel(x, y);
                    var dentroDeTolerancia =
                        Math.Abs(pixelReal.Red - pixelCanvas.Red) <= ToleranciaPorCanal &&
                        Math.Abs(pixelReal.Green - pixelCanvas.Green) <= ToleranciaPorCanal &&
                        Math.Abs(pixelReal.Blue - pixelCanvas.Blue) <= ToleranciaPorCanal;

                    Assert.True(dentroDeTolerancia,
                        $"{modo} en ({x},{y}): SkiaSharp={pixelReal} canvas={pixelCanvas}");
                }
            }
        }
    }
}
