using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Galería admin (Fase 1): entrega de thumb/preview (solo con la foto Lista), descarga del
    /// original limpio (ADR-06: único camino al original, admin autenticado) y borrado completo
    /// (fila + archivos). Todo scopeado por tenant.
    /// </summary>
    public class FotoGaleriaTests : IntegrationTestBase
    {
        public FotoGaleriaTests(TestWebApplicationFactory factory) : base(factory) { }

        #region Helpers

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<long> CrearGrupoAsync(HttpClient client)
        {
            var evento = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                new { id = 0, nombre = "Egresados 2026", estado = 0, tamanosPrecios = Array.Empty<object>() });
            await evento.ShouldBeOk();
            var eventoId = (await evento.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var grupo = await client.PostAsJsonAsync("/api/fotos/grupos/update",
                new { id = 0, eventoId, nombre = "7ºA", participantes = Array.Empty<object>() });
            await grupo.ShouldBeOk();
            return (await grupo.Content.ReadFromJsonAsync<GrupoDto>())!.Id;
        }

        private static byte[] CrearJpeg(int ancho = 1600, int alto = 1200)
        {
            using var imagen = new Image<Rgb24>(ancho, alto, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        /// <summary>Sube UN archivo grupal al grupo y devuelve el FotoDto.</summary>
        private static async Task<FotoDto> SubirAsync(HttpClient client, long grupoId, byte[] contenido,
            string nombre = "foto.jpg")
        {
            var multipart = new MultipartFormDataContent
            {
                { new StringContent(grupoId.ToString()), "grupoId" },
            };
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

        private async Task<string> RutaOriginalAsync(long fotoId)
        {
            var storageKey = await Factory.QueryScalarAsync<Guid>(
                $"SELECT StorageKey FROM fot_Fotos WHERE Id = {fotoId}");
            var eventoId = await CountAsync($"SELECT EventoId FROM fot_Fotos WHERE Id = {fotoId}");
            var contentRoot = Factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            return Path.Combine(contentRoot, "App_Data", "storage", "fotos", "originals",
                eventoId.ToString(), $"{storageKey:N}.jpg");
        }

        private static bool EsJpeg(byte[] bytes) => bytes.Length > 2 && bytes[0] == 0xFF && bytes[1] == 0xD8;

        /// <summary>Firma RIFF....WEBP de un archivo WebP.</summary>
        private static bool EsWebp(byte[] bytes) =>
            bytes.Length > 12
            && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';

        #endregion

        [Fact] // GAL-01 — foto Lista: thumb y preview 200 image/webp (y el preview pesa más que el thumb)
        public async Task Thumb_y_preview_se_sirven_cuando_la_foto_esta_lista()
        {
            using var client = await CreateTenantClientAsync();
            var grupoId = await CrearGrupoAsync(client);
            var foto = await SubirAsync(client, grupoId, CrearJpeg());
            await EsperarProcesamientoAsync(foto.Id);

            var thumb = await client.GetAsync($"/api/fotos/fotos/{foto.Id}/thumb");
            await thumb.ShouldBeOk();
            Assert.Equal("image/webp", thumb.Content.Headers.ContentType!.MediaType);
            var thumbBytes = await thumb.Content.ReadAsByteArrayAsync();
            Assert.True(EsWebp(thumbBytes), "el thumb no es un WebP");

            var preview = await client.GetAsync($"/api/fotos/fotos/{foto.Id}/preview");
            await preview.ShouldBeOk();
            var previewBytes = await preview.Content.ReadAsByteArrayAsync();
            Assert.True(EsWebp(previewBytes), "el preview no es un WebP");
            Assert.True(previewBytes.Length > thumbBytes.Length,
                "el preview (900px) debería pesar más que el thumb (300px)");
        }

        [Fact] // GAL-02 — sin derivados (foto en Error): thumb/preview 404, pero el original sigue descargable
        public async Task Sin_derivados_no_hay_thumb_pero_si_original()
        {
            using var client = await CreateTenantClientAsync();
            var grupoId = await CrearGrupoAsync(client);
            var foto = await SubirAsync(client, grupoId, "esto no es un jpg"u8.ToArray(), "malo.jpg");
            await EsperarProcesamientoAsync(foto.Id); // queda en Error

            await (await client.GetAsync($"/api/fotos/fotos/{foto.Id}/thumb"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
            await (await client.GetAsync($"/api/fotos/fotos/{foto.Id}/preview"))
                .ShouldBeStatus(HttpStatusCode.NotFound);

            // El original subido se conserva tal cual, aunque el procesamiento haya fallado.
            var original = await client.GetAsync($"/api/fotos/fotos/{foto.Id}/original");
            await original.ShouldBeOk();
        }

        [Fact] // GAL-03 — el original baja INTACTO (bit a bit, sin watermark) y con su nombre
        public async Task Original_baja_intacto_y_con_su_nombre()
        {
            using var client = await CreateTenantClientAsync();
            var grupoId = await CrearGrupoAsync(client);
            var subido = CrearJpeg(800, 600);
            var foto = await SubirAsync(client, grupoId, subido, "para-imprimir.jpg");

            var resp = await client.GetAsync($"/api/fotos/fotos/{foto.Id}/original");
            await resp.ShouldBeOk();

            var bajado = await resp.Content.ReadAsByteArrayAsync();
            Assert.Equal(subido, bajado); // ADR-01: el sistema jamás toca el original
            Assert.Contains("para-imprimir.jpg",
                resp.Content.Headers.ContentDisposition!.ToString());
        }

        [Fact] // GAL-04 — aislamiento: la foto de tenant 2 no existe para tenant 3 (media ni delete)
        public async Task Foto_de_otro_tenant_es_invisible()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var grupoId = await CrearGrupoAsync(tenant2);
            var foto = await SubirAsync(tenant2, grupoId, CrearJpeg());
            await EsperarProcesamientoAsync(foto.Id);

            using var tenant3 = await CreateTenantClientAsync(3);
            await (await tenant3.GetAsync($"/api/fotos/fotos/{foto.Id}/thumb"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
            await (await tenant3.GetAsync($"/api/fotos/fotos/{foto.Id}/original"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
            await (await tenant3.DeleteAsync($"/api/fotos/fotos/{foto.Id}"))
                .ShouldBeStatus(HttpStatusCode.NotFound);

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Fotos WHERE Id = {foto.Id}"));
        }

        [Fact] // GAL-05 — delete: borra la fila y los archivos; repetirlo da 404
        public async Task Delete_borra_fila_y_archivos()
        {
            using var client = await CreateTenantClientAsync();
            var grupoId = await CrearGrupoAsync(client);
            var foto = await SubirAsync(client, grupoId, CrearJpeg());
            await EsperarProcesamientoAsync(foto.Id);

            var rutaOriginal = await RutaOriginalAsync(foto.Id);
            Assert.True(File.Exists(rutaOriginal));

            var resp = await client.DeleteAsync($"/api/fotos/fotos/{foto.Id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Fotos WHERE Id = {foto.Id}"));
            Assert.False(File.Exists(rutaOriginal), "el original debía borrarse del storage");
            Assert.False(File.Exists(rutaOriginal.Replace(
                Path.Combine("originals"), Path.Combine("derived")).Replace(".jpg", "_thumb.webp")),
                "el thumb debía borrarse del storage");

            await (await client.DeleteAsync($"/api/fotos/fotos/{foto.Id}"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
