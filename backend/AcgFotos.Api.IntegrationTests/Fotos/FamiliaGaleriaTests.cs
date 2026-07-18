using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Galería de la sesión de familia (ADR-11, Fase 2, <c>api/fotos/familia/fotos</c>): consume el
    /// token real que emite el canje. Cubre lo que <see cref="FotoGaleriaTests"/> NO puede probar —
    /// el alcance viene de los claims del JWT, no de un parámetro — y que el token de familia ya no
    /// se rechaza en <c>OnTokenValidated</c> (antes de este ítem, fallaba el chequeo de SecurityStamp).
    /// </summary>
    public class FamiliaGaleriaTests : IntegrationTestBase
    {
        public FamiliaGaleriaTests(TestWebApplicationFactory factory) : base(factory) { }

        #region Helpers

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        /// <summary>Evento Publicado + grupo con dos participantes (para probar aislamiento entre ellos).</summary>
        private static async Task<(long GrupoId, ParticipanteDto A, ParticipanteDto B)> CrearGrupoConDosParticipantesAsync(
            HttpClient client)
        {
            var eventoResp = await client.PostAsJsonAsync("/api/fotos/eventos/update", new
            {
                id = 0,
                nombre = "Egresados 2026",
                estado = (int)EstadoEvento.Publicado,
                tamanosPrecios = Array.Empty<object>(),
            });
            await eventoResp.ShouldBeOk();
            var eventoId = (await eventoResp.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var grupoResp = await client.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºB",
                participantes = new[] { new { id = 0, nombre = "Ana Pérez" }, new { id = 0, nombre = "Beto Ruiz" } },
            });
            await grupoResp.ShouldBeOk();
            var grupo = (await grupoResp.Content.ReadFromJsonAsync<GrupoDto>())!;
            var a = grupo.Participantes.Single(p => p.Nombre == "Ana Pérez");
            var b = grupo.Participantes.Single(p => p.Nombre == "Beto Ruiz");
            return (grupo.Id, a, b);
        }

        private static byte[] CrearJpeg(int ancho = 800, int alto = 600)
        {
            using var imagen = new Image<Rgb24>(ancho, alto, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private static async Task<FotoDto> SubirAsync(
            HttpClient client, long grupoId, long? participanteId, byte[] contenido, string nombre = "foto.jpg")
        {
            var multipart = new MultipartFormDataContent
            {
                { new StringContent(grupoId.ToString()), "grupoId" },
            };
            if (participanteId is not null)
            {
                multipart.Add(new StringContent(participanteId.Value.ToString()), "participanteId");
            }
            var archivo = new ByteArrayContent(contenido);
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipart.Add(archivo, "archivos", nombre);

            var resp = await client.PostAsync("/api/fotos/fotos/upload", multipart);
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
        }

        private async Task EsperarProcesamientoAsync(long fotoId)
        {
            for (var intento = 0; intento < 120; intento++) // hasta 30 s
            {
                var estado = (EstadoProcesamientoFoto)await CountAsync(
                    $"SELECT EstadoProcesamiento FROM fot_Fotos WHERE Id = {fotoId}");
                if (estado != EstadoProcesamientoFoto.Pendiente)
                {
                    return;
                }

                await Task.Delay(250);
            }

            Assert.Fail($"La foto {fotoId} sigue Pendiente tras 30 s: el worker no la procesó.");
        }

        private static bool EsWebp(byte[] bytes) =>
            bytes.Length > 12
            && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';

        /// <summary>Canjea el código (endpoint anónimo real) y arma un cliente con el JWT de familia.</summary>
        private async Task<HttpClient> CanjearAsync(string codigo)
        {
            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeOk();
            var dto = (await resp.Content.ReadFromJsonAsync<CanjeResultDto>())!;

            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.Token);
            return client;
        }

        #endregion

        [Fact] // FAMGAL-01 — la familia ve sus individuales + las grupales del grupo; nada más
        public async Task Lista_individuales_y_grupales_de_la_sesion()
        {
            using var admin = await CreateTenantClientAsync();
            var (grupoId, ana, beto) = await CrearGrupoConDosParticipantesAsync(admin);

            var individualAna = await SubirAsync(admin, grupoId, ana.Id, CrearJpeg(), "ana.jpg");
            var individualBeto = await SubirAsync(admin, grupoId, beto.Id, CrearJpeg(), "beto.jpg");
            var grupal = await SubirAsync(admin, grupoId, null, CrearJpeg(), "grupal.jpg");
            await EsperarProcesamientoAsync(individualAna.Id);
            await EsperarProcesamientoAsync(individualBeto.Id);
            await EsperarProcesamientoAsync(grupal.Id);

            using var familia = await CanjearAsync(ana.CodigoAcceso!);

            var resp = await familia.GetAsync("/api/fotos/familia/fotos");
            await resp.ShouldBeOk();
            var fotos = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!;

            Assert.Equal(
                new[] { individualAna.Id, grupal.Id }.OrderBy(x => x),
                fotos.Select(f => f.Id).OrderBy(x => x));
            Assert.DoesNotContain(fotos, f => f.Id == individualBeto.Id);
        }

        [Fact] // FAMGAL-02 — thumb/preview de la propia foto: 200 image/webp
        public async Task Thumb_y_preview_de_foto_propia_se_sirven()
        {
            using var admin = await CreateTenantClientAsync();
            var (grupoId, ana, _) = await CrearGrupoConDosParticipantesAsync(admin);
            var foto = await SubirAsync(admin, grupoId, ana.Id, CrearJpeg());
            await EsperarProcesamientoAsync(foto.Id);

            using var familia = await CanjearAsync(ana.CodigoAcceso!);

            var thumb = await familia.GetAsync($"/api/fotos/familia/fotos/{foto.Id}/thumb");
            await thumb.ShouldBeOk();
            Assert.Equal("image/webp", thumb.Content.Headers.ContentType!.MediaType);
            Assert.True(EsWebp(await thumb.Content.ReadAsByteArrayAsync()));

            var preview = await familia.GetAsync($"/api/fotos/familia/fotos/{foto.Id}/preview");
            await preview.ShouldBeOk();
            Assert.True(EsWebp(await preview.Content.ReadAsByteArrayAsync()));
        }

        [Fact] // FAMGAL-03 — thumb/preview de la foto individual de OTRO participante: 404 (no una pista de "existe pero no es tuya")
        public async Task Thumb_de_foto_ajena_da_404()
        {
            using var admin = await CreateTenantClientAsync();
            var (grupoId, ana, beto) = await CrearGrupoConDosParticipantesAsync(admin);
            var fotoDeBeto = await SubirAsync(admin, grupoId, beto.Id, CrearJpeg());
            await EsperarProcesamientoAsync(fotoDeBeto.Id);

            using var familia = await CanjearAsync(ana.CodigoAcceso!);

            await (await familia.GetAsync($"/api/fotos/familia/fotos/{fotoDeBeto.Id}/thumb"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
            await (await familia.GetAsync($"/api/fotos/familia/fotos/{fotoDeBeto.Id}/preview"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // FAMGAL-04 — foto todavía Pendiente (el worker no la procesó): invisible para la familia
        public async Task Foto_pendiente_no_aparece_para_la_familia()
        {
            using var admin = await CreateTenantClientAsync();
            var (grupoId, ana, _) = await CrearGrupoConDosParticipantesAsync(admin);
            var foto = await SubirAsync(admin, grupoId, ana.Id, CrearJpeg());
            // A propósito: NO se espera el procesamiento, la foto sigue Pendiente.

            using var familia = await CanjearAsync(ana.CodigoAcceso!);

            var resp = await familia.GetAsync("/api/fotos/familia/fotos");
            await resp.ShouldBeOk();
            var fotos = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!;
            Assert.DoesNotContain(fotos, f => f.Id == foto.Id);

            await (await familia.GetAsync($"/api/fotos/familia/fotos/{foto.Id}/thumb"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // FAMGAL-05 — no existe /original en la galería de familia (ADR-06: nunca se expone acá)
        public async Task No_existe_endpoint_de_original_para_familia()
        {
            using var admin = await CreateTenantClientAsync();
            var (grupoId, ana, _) = await CrearGrupoConDosParticipantesAsync(admin);
            var foto = await SubirAsync(admin, grupoId, ana.Id, CrearJpeg());
            await EsperarProcesamientoAsync(foto.Id);

            using var familia = await CanjearAsync(ana.CodigoAcceso!);

            await (await familia.GetAsync($"/api/fotos/familia/fotos/{foto.Id}/original"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
