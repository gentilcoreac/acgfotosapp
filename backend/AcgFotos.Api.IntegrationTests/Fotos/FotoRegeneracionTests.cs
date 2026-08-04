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
    /// Regeneración explícita de derivados por evento (spec `regeneracion-derivados`): reusa el
    /// worker existente (D9) — el endpoint sólo marca Pendiente y encola, con el conteo informado
    /// ANTES para el diálogo de confirmación.
    /// </summary>
    public class FotoRegeneracionTests : IntegrationTestBase
    {
        public FotoRegeneracionTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<(long EventoId, long GrupoId)> CrearEventoConGrupoAsync(HttpClient client)
        {
            var evento = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                new { id = 0, nombre = "Egresados 2026", estado = 0, tamanosPrecios = Array.Empty<object>() });
            await evento.ShouldBeOk();
            var eventoId = (await evento.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var grupo = await client.PostAsJsonAsync("/api/fotos/grupos/update",
                new { id = 0, eventoId, nombre = "7ºA", participantes = Array.Empty<object>() });
            await grupo.ShouldBeOk();
            return (eventoId, (await grupo.Content.ReadFromJsonAsync<GrupoDto>())!.Id);
        }

        private static byte[] CrearJpeg(int ancho = 1600, int alto = 1200)
        {
            using var imagen = new Image<Rgb24>(ancho, alto, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private static async Task<FotoDto> SubirAsync(HttpClient client, long grupoId, byte[] contenido)
        {
            var multipart = new MultipartFormDataContent
            {
                { new StringContent(grupoId.ToString()), "grupoId" },
            };
            var archivo = new ByteArrayContent(contenido);
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipart.Add(archivo, "archivos", "foto.jpg");

            var resp = await client.PostAsync("/api/fotos/fotos/upload", multipart);
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
        }

        private async Task EsperarEstadoDistintoDePendienteAsync(long fotoId)
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

        [Fact] // FRG-01 — el conteo refleja las fotos ya procesadas (Lista), no las Pendientes
        public async Task Conteo_cuenta_solo_fotos_ya_procesadas()
        {
            using var client = await CreateTenantClientAsync();
            var (eventoId, grupoId) = await CrearEventoConGrupoAsync(client);
            var foto = await SubirAsync(client, grupoId, CrearJpeg());
            await EsperarEstadoDistintoDePendienteAsync(foto.Id);

            var resp = await client.GetAsync($"/api/fotos/fotos/regenerar/conteo?eventoId={eventoId}");
            await resp.ShouldBeOk();

            Assert.Equal(1, await resp.Content.ReadFromJsonAsync<int>());
        }

        [Fact] // FRG-02 — evento sin fotos procesadas: conteo 0, no encola nada
        public async Task Evento_sin_fotos_procesadas_no_encola_nada()
        {
            using var client = await CreateTenantClientAsync();
            var (eventoId, _) = await CrearEventoConGrupoAsync(client);

            var conteo = await client.GetAsync($"/api/fotos/fotos/regenerar/conteo?eventoId={eventoId}");
            Assert.Equal(0, await conteo.Content.ReadFromJsonAsync<int>());

            var regenerar = await client.PostAsync($"/api/fotos/fotos/regenerar?eventoId={eventoId}", null);
            await regenerar.ShouldBeOk();
            Assert.Equal(0, await regenerar.Content.ReadFromJsonAsync<int>());
        }

        [Fact] // FRG-03 — regenerar marca la foto Pendiente, la encola, y el worker la vuelve a dejar Lista sin tocar el original
        public async Task Regenerar_marca_pendiente_encola_y_no_toca_el_original()
        {
            using var client = await CreateTenantClientAsync();
            var (eventoId, grupoId) = await CrearEventoConGrupoAsync(client);
            var foto = await SubirAsync(client, grupoId, CrearJpeg());
            await EsperarEstadoDistintoDePendienteAsync(foto.Id);
            var originalAntes = await (await client.GetAsync($"/api/fotos/fotos/{foto.Id}/original"))
                .Content.ReadAsByteArrayAsync();

            var resp = await client.PostAsync($"/api/fotos/fotos/regenerar?eventoId={eventoId}", null);
            await resp.ShouldBeOk();
            Assert.Equal(1, await resp.Content.ReadFromJsonAsync<int>());

            await EsperarEstadoDistintoDePendienteAsync(foto.Id);

            Assert.Equal((int)EstadoProcesamientoFoto.Lista, await CountAsync(
                $"SELECT EstadoProcesamiento FROM fot_Fotos WHERE Id = {foto.Id}"));

            // El original sigue sirviéndose igual (mismos bytes, ADR-06) tras regenerar los derivados.
            var originalDespues = await (await client.GetAsync($"/api/fotos/fotos/{foto.Id}/original"))
                .Content.ReadAsByteArrayAsync();
            Assert.Equal(originalAntes, originalDespues);
        }

        [Fact] // FRG-04 — evento de otro tenant: rechazado, no encola
        public async Task Evento_de_otro_tenant_es_rechazado()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var (eventoId, grupoId) = await CrearEventoConGrupoAsync(tenant2);
            var foto = await SubirAsync(tenant2, grupoId, CrearJpeg());
            await EsperarEstadoDistintoDePendienteAsync(foto.Id);

            using var tenant3 = await CreateTenantClientAsync(3);

            await (await tenant3.GetAsync($"/api/fotos/fotos/regenerar/conteo?eventoId={eventoId}"))
                .ShouldBeStatus(HttpStatusCode.BadRequest);
            await (await tenant3.PostAsync($"/api/fotos/fotos/regenerar?eventoId={eventoId}", null))
                .ShouldBeStatus(HttpStatusCode.BadRequest);

            Assert.Equal((int)EstadoProcesamientoFoto.Lista, await CountAsync(
                $"SELECT EstadoProcesamiento FROM fot_Fotos WHERE Id = {foto.Id}"));
        }
    }
}
