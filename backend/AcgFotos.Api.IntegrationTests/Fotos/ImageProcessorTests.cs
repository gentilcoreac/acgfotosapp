using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Fotos.Domain.Entities;
using AcgFotos.Fotos.Infrastructure.Imaging;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Humo del pipeline de derivados (ADR-01 capa 1) tras ADR-15/ADR-16. Tests puros: sin host ni
    /// DB — la cascada de resolución (evento → tenant → OpcionesFotos) es responsabilidad de
    /// <c>FotoProcesadorAppService</c>, no de este processor; acá se le pasan capas ya resueltas.
    /// Las imágenes de prueba se generan en memoria para que la verificación sea determinística.
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

        /// <summary>Capa de prueba: un cuadrado blanco opaco — la opacidad la aplica la composición, no el asset.</summary>
        private static byte[] CrearCapaBlanca(int lado = 100)
        {
            using var img = new Image<Rgba32>(lado, lado, new Rgba32(255, 255, 255, 255));
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        private static OpcionesDerivados Opciones(IReadOnlyList<CapaComposicion>? capas = null) => new()
        {
            Capas = capas ?? [],
        };

        /// <summary>
        /// WebP es lossy (ADR-01): incluso un color plano sin marca puede correrse unos pocos
        /// niveles por canal al recomprimir. Estas comparaciones verifican intención (marcado vs.
        /// no marcado), no bytes exactos.
        /// </summary>
        private static void AssertColorAprox(Rgb24 esperado, Rgb24 real, int tolerancia = 15)
        {
            Assert.True(
                Math.Abs(esperado.R - real.R) <= tolerancia &&
                Math.Abs(esperado.G - real.G) <= tolerancia &&
                Math.Abs(esperado.B - real.B) <= tolerancia,
                $"esperado ~{esperado}, real {real} (tolerancia {tolerancia})");
        }

        [Fact]
        public async Task Genera_preview_y_thumb_con_los_lados_mayores_pedidos()
        {
            using var original = CrearJpegPlano(4000, 3000);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            Assert.Equal(4000, derivados.AnchoOriginal);
            Assert.Equal(3000, derivados.AltoOriginal);

            using var preview = Image.Load(derivados.Preview);
            Assert.Equal(900, Math.Max(preview.Width, preview.Height));
            // Conserva el aspecto 4:3.
            Assert.Equal(675, Math.Min(preview.Width, preview.Height));

            using var thumb = Image.Load(derivados.Thumb);
            Assert.Equal(300, Math.Max(thumb.Width, thumb.Height));
        }

        [Fact]
        public async Task No_agranda_una_imagen_menor_al_lado_pedido()
        {
            using var original = CrearJpegPlano(800, 600);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            using var preview = Image.Load(derivados.Preview);
            Assert.Equal(800, preview.Width);
            Assert.Equal(600, preview.Height);
        }

        [Fact]
        public async Task Sin_capas_no_altera_pixeles()
        {
            using var original = CrearJpegPlano(400, 300);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            using var preview = Image.Load<Rgb24>(derivados.Preview);
            preview.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var fila = accessor.GetRowSpan(y);
                    for (var x = 0; x < fila.Length; x++)
                    {
                        AssertColorAprox(new Rgb24(40, 90, 160), fila[x]);
                    }
                }
            });
        }

        [Fact]
        public async Task La_capa_repetida_altera_pixeles_en_toda_la_imagen()
        {
            using var original = CrearJpegPlano(2000, 1500);
            var capas = new[]
            {
                new CapaComposicion
                {
                    Asset = CrearCapaBlanca(),
                    ModoColocacion = ModoColocacionMarcaAgua.Repetida,
                    EscalaPorcentaje = 8f,
                    Opacidad = 0.5f,
                    ModoFusion = ModoFusionMarcaAgua.Normal,
                },
            };

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones(capas));

            // Sobre un color plano, los píxeles "más claros que el fondo" solo pueden ser la capa
            // blanca al 50%. Se exige presencia en los CUATRO cuadrantes: una marca solo en el
            // centro o en una esquina se recorta fácil y no cumple ADR-01.
            using var preview = Image.Load<Rgb24>(derivados.Preview);
            var cuadrantesConMarca = new bool[2, 2];
            preview.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var fila = accessor.GetRowSpan(y);
                    for (var x = 0; x < fila.Length; x++)
                    {
                        // Umbral holgado sobre el canal rojo (fondo=40 + capa blanca al 50% lo sube).
                        if (fila[x].R > 90)
                        {
                            cuadrantesConMarca[y * 2 / accessor.Height, x * 2 / fila.Length] = true;
                        }
                    }
                }
            });

            Assert.True(cuadrantesConMarca[0, 0], "sin marca en el cuadrante superior izquierdo");
            Assert.True(cuadrantesConMarca[0, 1], "sin marca en el cuadrante superior derecho");
            Assert.True(cuadrantesConMarca[1, 0], "sin marca en el cuadrante inferior izquierdo");
            Assert.True(cuadrantesConMarca[1, 1], "sin marca en el cuadrante inferior derecho");
        }

        [Fact]
        public async Task La_capa_en_posicion_fija_respeta_el_margen()
        {
            using var original = CrearJpegPlano(1000, 1000);
            var capas = new[]
            {
                new CapaComposicion
                {
                    Asset = CrearCapaBlanca(),
                    ModoColocacion = ModoColocacionMarcaAgua.PosicionFija,
                    Posicion = PosicionMarcaAgua.ArribaIzquierda,
                    EscalaPorcentaje = 10f,
                    MargenPorcentaje = 5f,
                    Opacidad = 1f,
                    ModoFusion = ModoFusionMarcaAgua.Normal,
                },
            };

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones(capas));

            using var preview = Image.Load<Rgb24>(derivados.Preview);
            // preview.Width=900 (resize de 1000→900), margen=5%=45px, asset 100px al 10%=90px de
            // ancho ⇒ la marca ocupa aprox. x∈[45,135]. Puntos con margen de seguridad amplio a cada
            // lado del borde (no a 1-2px, ahí el bloque de WebP puede sangrar) para no depender de
            // exactitud de compresión: bien afuera (esquina) vs. bien adentro del área marcada.
            AssertColorAprox(new Rgb24(40, 90, 160), preview[15, 15]);
            AssertColorAprox(new Rgb24(255, 255, 255), preview[80, 80]);
        }

        [Fact]
        public async Task Varias_capas_se_componen_en_orden()
        {
            using var original = CrearJpegPlano(500, 500);
            using var capaChica = new Image<Rgba32>(50, 50, new Rgba32(255, 255, 255, 255));
            using var msChica = new MemoryStream();
            capaChica.SaveAsPng(msChica);

            // Dos capas en posición fija superpuestas: la de Orden=1 debe quedar "arriba" de la de Orden=0
            // (ambas blancas opacas, así que el resultado final es indistinguible del blanco puro donde
            // se solapan — lo que importa es que no tire una excepción y compone las dos).
            var capas = new[]
            {
                new CapaComposicion
                {
                    Asset = msChica.ToArray(), Orden = 1, ModoColocacion = ModoColocacionMarcaAgua.PosicionFija,
                    Posicion = PosicionMarcaAgua.Centro, EscalaPorcentaje = 20f, Opacidad = 1f,
                },
                new CapaComposicion
                {
                    Asset = CrearCapaBlanca(), Orden = 0, ModoColocacion = ModoColocacionMarcaAgua.PosicionFija,
                    Posicion = PosicionMarcaAgua.Centro, EscalaPorcentaje = 30f, Opacidad = 1f,
                },
            };

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones(capas));

            using var preview = Image.Load<Rgb24>(derivados.Preview);
            var centro = preview.Width / 2;
            AssertColorAprox(new Rgb24(255, 255, 255), preview[centro, centro]);
        }

        [Fact]
        public async Task No_agranda_el_asset_de_una_capa_mas_alla_de_su_tamano_natural()
        {
            using var original = CrearJpegPlano(4000, 3000);
            // La composición corre sobre el DERIVADO ya redimensionado (900px de lado mayor, no los
            // 4000px del original): 80% de 900px = 720px pedidos contra un asset natural de 50px —
            // el resultado debe quedar acotado a 50px, no estirado a 720px.
            var capas = new[]
            {
                new CapaComposicion
                {
                    Asset = CrearCapaBlanca(50),
                    ModoColocacion = ModoColocacionMarcaAgua.PosicionFija,
                    Posicion = PosicionMarcaAgua.ArribaIzquierda,
                    EscalaPorcentaje = 80f,
                    Opacidad = 1f,
                },
            };

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones(capas));

            using var preview = Image.Load<Rgb24>(derivados.Preview);
            // Bien adentro de la franja de 50px (asset natural): marcado. Bien afuera (con margen de
            // seguridad amplio, no pegado al borde): sin marca. Si el asset se hubiera "agrandado" a
            // los 720px pedidos, este segundo punto también saldría blanco.
            AssertColorAprox(new Rgb24(255, 255, 255), preview[20, 20]);
            AssertColorAprox(new Rgb24(40, 90, 160), preview[200, 20]);
        }

        [Fact]
        public async Task Limpia_el_EXIF_del_original_GPS_y_equipo_en_ambos_derivados()
        {
            using var imagen = new Image<Rgb24>(800, 600, new Rgb24(40, 90, 160));
            imagen.Metadata.ExifProfile = new ExifProfile();
            imagen.Metadata.ExifProfile.SetValue(ExifTag.Model, "Canon EOS R5");
            imagen.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, "S");
            using var original = new MemoryStream();
            imagen.SaveAsJpeg(original);
            original.Position = 0;

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones());

            using var preview = Image.Load(derivados.Preview);
            using var thumb = Image.Load(derivados.Thumb);
            Assert.Null(preview.Metadata.ExifProfile);
            Assert.Null(thumb.Metadata.ExifProfile);
        }

        [Fact]
        public async Task Contenido_no_imagen_tira_ImagenInvalidaException()
        {
            using var noImagen = new MemoryStream("esto no es un jpg"u8.ToArray());

            await Assert.ThrowsAsync<ImagenInvalidaException>(
                () => Processor.GenerarDerivadosAsync(noImagen, Opciones()));
        }

        /// <summary>
        /// Cuenta las repeticiones de una capa blanca sobre un fondo plano contando "islas": el pixel
        /// marcado cuyo vecino izquierdo y superior no lo están abre una marca nueva. Alcanza para
        /// comparar densidades entre configuraciones, que es lo que verifican los tests de separación.
        /// </summary>
        private static int ContarMarcas(byte[] webp)
        {
            using var img = Image.Load<Rgb24>(webp);
            var marcado = new bool[img.Height, img.Width];
            img.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var fila = accessor.GetRowSpan(y);
                    for (var x = 0; x < fila.Length; x++)
                    {
                        marcado[y, x] = fila[x].R > 90;
                    }
                }
            });

            var islas = 0;
            for (var y = 0; y < img.Height; y++)
            {
                for (var x = 0; x < img.Width; x++)
                {
                    if (marcado[y, x] && (x == 0 || !marcado[y, x - 1]) && (y == 0 || !marcado[y - 1, x]))
                    {
                        islas++;
                    }
                }
            }
            return islas;
        }

        // Asset holgado: la composición nunca escala hacia arriba (ADR-15 §8), así que un asset chico
        // recortaría la escala pedida y los tamaños a comparar dejarían de ser distintos de verdad.
        private static CapaComposicion CapaRepetida(float escala, float separacion) => new()
        {
            Asset = CrearCapaBlanca(400),
            ModoColocacion = ModoColocacionMarcaAgua.Repetida,
            EscalaPorcentaje = escala,
            SeparacionPorcentaje = separacion,
            Opacidad = 0.5f,
            ModoFusion = ModoFusionMarcaAgua.Normal,
        };

        [Fact]
        public async Task Achicar_la_marca_con_la_misma_separacion_no_cambia_cuantas_hay()
        {
            using var grande = CrearJpegPlano(2000, 1500);
            using var chica = CrearJpegPlano(2000, 1500);

            var conMarcaGrande = await Processor.GenerarDerivadosAsync(grande, Opciones([CapaRepetida(20f, 30f)]));
            var conMarcaChica = await Processor.GenerarDerivadosAsync(chica, Opciones([CapaRepetida(10f, 30f)]));

            // Antes de que la separación fuera propia de la capa, el paso salía del tamaño del tile:
            // la mitad de escala daba el doble de marcas. Ahora la diferencia sólo puede venir del
            // borde (una marca al límite entra o no entra según su tamaño), nunca de la densidad.
            var conGrande = ContarMarcas(conMarcaGrande.Preview);
            var conChica = ContarMarcas(conMarcaChica.Preview);
            Assert.True(
                Math.Abs(conGrande - conChica) <= 1,
                $"achicar la marca cambió la cantidad de repeticiones: {conGrande} contra {conChica}");
        }

        [Fact]
        public async Task Separar_mas_reduce_la_cantidad_de_marcas()
        {
            using var junto = CrearJpegPlano(2000, 1500);
            using var separado = CrearJpegPlano(2000, 1500);

            var conMarcasJuntas = await Processor.GenerarDerivadosAsync(junto, Opciones([CapaRepetida(15f, 20f)]));
            var conMarcasSeparadas = await Processor.GenerarDerivadosAsync(separado, Opciones([CapaRepetida(15f, 50f)]));

            Assert.True(
                ContarMarcas(conMarcasSeparadas.Preview) < ContarMarcas(conMarcasJuntas.Preview),
                "separar más debería dejar menos marcas sobre la foto");
        }

        [Fact]
        public async Task Sin_separacion_explicita_la_foto_igual_sale_marcada()
        {
            using var original = CrearJpegPlano(2000, 1500);

            var derivados = await Processor.GenerarDerivadosAsync(original, Opciones([CapaRepetida(15f, 0f)]));

            Assert.True(ContarMarcas(derivados.Preview) > 0, "una capa sin separación no puede dejar la foto sin marca");
        }
    }
}
