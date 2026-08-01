using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using AcgFotos.Core.Exceptions;
using AcgFotos.Fotos.Application.Procesamiento;
using AcgFotos.Fotos.Infrastructure.Imaging;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>Tests puros del validador de assets de capa (ADR-15 §7, design D5): sin host ni DB.</summary>
    public class ValidadorAssetMarcaAguaTests
    {
        private static ValidadorAssetMarcaAgua Validador(OpcionesFotos? opciones = null) => new(opciones ?? new OpcionesFotos());

        private static byte[] PngConAlfa(int ancho = 50, int alto = 50)
        {
            using var img = new Image<Rgba32>(ancho, alto, new Rgba32(255, 255, 255, 128));
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        private static byte[] PngSinAlfa(int ancho = 50, int alto = 50)
        {
            using var img = new Image<Rgb24>(ancho, alto, new Rgb24(255, 255, 255));
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        [Fact]
        public async Task Acepta_un_png_con_canal_alfa()
        {
            var resultado = await Validador().ValidarAsync(PngConAlfa(80, 60));

            Assert.Equal(80, resultado.AnchoPx);
            Assert.Equal(60, resultado.AltoPx);
            Assert.True(resultado.TieneCanalAlfa);
        }

        [Fact]
        public async Task Detecta_un_png_sin_canal_alfa()
        {
            var resultado = await Validador().ValidarAsync(PngSinAlfa());

            Assert.False(resultado.TieneCanalAlfa);
        }

        [Fact]
        public async Task Rechaza_contenido_que_no_es_una_imagen()
        {
            var contenido = "esto no es un png"u8.ToArray();

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => Validador().ValidarAsync(contenido));
            Assert.Contains("no es una imagen válida", ex.Message);
        }

        [Fact]
        public async Task Rechaza_un_formato_que_no_es_png()
        {
            using var img = new Image<Rgb24>(50, 50, new Rgb24(255, 255, 255));
            using var ms = new MemoryStream();
            img.SaveAsJpeg(ms);

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => Validador().ValidarAsync(ms.ToArray()));
            Assert.Contains("PNG", ex.Message);
        }

        [Fact]
        public async Task Rechaza_un_archivo_que_excede_el_peso_maximo()
        {
            var opciones = new OpcionesFotos { PesoMaximoAssetMarcaAguaBytes = 100 };
            var contenido = new byte[200];

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => Validador(opciones).ValidarAsync(contenido));
            Assert.Contains("KB", ex.Message);
        }

        [Fact]
        public async Task Rechaza_dimensiones_que_exceden_el_techo_configurado()
        {
            // El techo se verifica leyendo sólo la cabecera (Image.IdentifyAsync), antes de
            // decodificar el bitmap completo — acá se prueba el resultado observable (el rechazo),
            // no el orden interno de las llamadas.
            var opciones = new OpcionesFotos { AnchoMaximoAssetMarcaAguaPx = 40, AltoMaximoAssetMarcaAguaPx = 40 };
            var contenido = PngConAlfa(80, 80);

            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => Validador(opciones).ValidarAsync(contenido));
            Assert.Contains("80×80", ex.Message);
        }

        [Fact]
        public async Task Acepta_dimensiones_dentro_del_techo_configurado()
        {
            var opciones = new OpcionesFotos { AnchoMaximoAssetMarcaAguaPx = 100, AltoMaximoAssetMarcaAguaPx = 100 };

            var resultado = await Validador(opciones).ValidarAsync(PngConAlfa(80, 80));

            Assert.Equal(80, resultado.AnchoPx);
        }

        [Fact]
        public void EvaluarEscala_detecta_un_logo_mas_chico_de_lo_que_pide_la_escala()
        {
            // Foto de referencia 1600px (default), escala 75% ⇒ necesita 1200px; el logo tiene 300.
            var evaluacion = Validador().EvaluarEscala(anchoAssetPx: 300, escalaPorcentaje: 75f);

            Assert.False(evaluacion.Alcanza);
            Assert.Equal(1200, evaluacion.AnchoNecesarioPx);
        }

        [Fact]
        public void EvaluarEscala_acepta_un_logo_que_alcanza_para_la_escala()
        {
            var evaluacion = Validador().EvaluarEscala(anchoAssetPx: 1200, escalaPorcentaje: 50f, anchoReferenciaFotoPx: 1600);

            Assert.True(evaluacion.Alcanza);
            Assert.Equal(800, evaluacion.AnchoNecesarioPx);
        }
    }
}
