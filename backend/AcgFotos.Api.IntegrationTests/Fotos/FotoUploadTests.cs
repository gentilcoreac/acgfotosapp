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
    /// Upload masivo (Fase 1): el original va al storage privado, la fila nace Pendiente y el
    /// worker de background genera thumb + preview con watermark y la deja Lista (o Error si el
    /// contenido no es imagen). Los tests esperan el resultado del worker con polling sobre la DB
    /// (el host de tests corre el hosted service real). Mismo esquema de tenant que el resto:
    /// root + <c>SimulatedTenant:2</c>.
    /// </summary>
    public class FotoUploadTests : IntegrationTestBase
    {
        public FotoUploadTests(TestWebApplicationFactory factory) : base(factory) { }

        #region Helpers

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        /// <summary>Arma evento + grupo (+ participante "Ana") y devuelve (grupoId, participanteId).</summary>
        private static async Task<(long GrupoId, long ParticipanteId)> CrearGrupoConParticipanteAsync(HttpClient client)
        {
            var evento = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                new { id = 0, nombre = "Egresados 2026", estado = 0, tamanosPrecios = Array.Empty<object>() });
            await evento.ShouldBeOk();
            var eventoId = (await evento.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var grupo = await client.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºA",
                participantes = new[] { new { id = 0, nombre = "Ana" } },
            });
            await grupo.ShouldBeOk();
            var grupoDto = (await grupo.Content.ReadFromJsonAsync<GrupoDto>())!;

            return (grupoDto.Id, grupoDto.Participantes.Single().Id);
        }

        private static byte[] CrearJpeg(int ancho = 1600, int alto = 1200)
        {
            using var imagen = new Image<Rgb24>(ancho, alto, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private static MultipartFormDataContent Multipart(long grupoId, long? participanteId,
            params (string Nombre, byte[] Contenido)[] archivos)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(grupoId.ToString()), "grupoId" },
            };
            if (participanteId is not null)
            {
                content.Add(new StringContent(participanteId.ToString()!), "participanteId");
            }

            foreach (var (nombre, bytes) in archivos)
            {
                var archivo = new ByteArrayContent(bytes);
                archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(archivo, "archivos", nombre);
            }

            return content;
        }

        /// <summary>Espera a que el worker saque la foto de Pendiente y devuelve el estado final.</summary>
        private async Task<EstadoProcesamientoFoto> EsperarProcesamientoAsync(long fotoId)
        {
            for (var intento = 0; intento < 120; intento++) // hasta 30 s
            {
                var estado = (EstadoProcesamientoFoto)await CountAsync(
                    $"SELECT EstadoProcesamiento FROM fot_Fotos WHERE Id = {fotoId}");
                if (estado != EstadoProcesamientoFoto.Pendiente)
                {
                    return estado;
                }

                await Task.Delay(250);
            }

            Assert.Fail($"La foto {fotoId} sigue Pendiente tras 30 s: el worker no la procesó.");
            return default; // inalcanzable
        }

        private string RutaStorage(string keyRelativa)
        {
            var contentRoot = Factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            return Path.Combine(contentRoot, "App_Data", "storage",
                keyRelativa.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        #endregion

        [Fact] // FUP-01 — happy: sube 2 al participante, quedan Pendiente y el worker las deja Lista con derivados
        public async Task Upload_a_participante_procesa_y_deja_derivados()
        {
            using var client = await CreateTenantClientAsync(); // tenant 2
            var (grupoId, participanteId) = await CrearGrupoConParticipanteAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoId, participanteId, ("ana-01.jpg", CrearJpeg()), ("ana-02.jpg", CrearJpeg(800, 600))));
            await resp.ShouldBeOk();

            var dtos = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!;
            Assert.Equal(2, dtos.Count);
            Assert.All(dtos, f => Assert.Equal(participanteId, f.ParticipanteId));

            Assert.Equal(2, await CountAsync(
                $"SELECT COUNT(*) FROM fot_Fotos WHERE GrupoId = {grupoId} AND TenantId = 2"));

            foreach (var dto in dtos)
            {
                Assert.Equal(EstadoProcesamientoFoto.Lista, await EsperarProcesamientoAsync(dto.Id));
            }

            // Dimensiones registradas por el worker (las del original).
            var chica = dtos.Single(f => f.NombreArchivoOriginal == "ana-02.jpg");
            Assert.Equal(800, await CountAsync($"SELECT Ancho FROM fot_Fotos WHERE Id = {chica.Id}"));
            Assert.Equal(600, await CountAsync($"SELECT Alto FROM fot_Fotos WHERE Id = {chica.Id}"));

            // Original + thumb + preview en el storage privado (FileSystem en dev/tests).
            var storageKey = await Factory.QueryScalarAsync<Guid>(
                $"SELECT StorageKey FROM fot_Fotos WHERE Id = {chica.Id}");
            var eventoId = await CountAsync($"SELECT EventoId FROM fot_Fotos WHERE Id = {chica.Id}");
            Assert.True(File.Exists(RutaStorage($"fotos/originals/{eventoId}/{storageKey:N}.jpg")), "falta el original");
            Assert.True(File.Exists(RutaStorage($"fotos/derived/{eventoId}/{storageKey:N}_thumb.webp")), "falta el thumb");
            Assert.True(File.Exists(RutaStorage($"fotos/derived/{eventoId}/{storageKey:N}_preview.webp")), "falta el preview");
        }

        [Fact] // FUP-02 — upload sin participanteId: foto GRUPAL del grupo (ParticipanteId null)
        public async Task Upload_sin_participante_es_grupal()
        {
            using var client = await CreateTenantClientAsync();
            var (grupoId, _) = await CrearGrupoConParticipanteAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoId, participanteId: null, ("grupal.jpg", CrearJpeg())));
            await resp.ShouldBeOk();

            var dto = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
            Assert.Null(dto.ParticipanteId);
            Assert.Equal(1, await CountAsync(
                $"SELECT COUNT(*) FROM fot_Fotos WHERE Id = {dto.Id} AND ParticipanteId IS NULL"));
        }

        [Fact] // FUP-03 — contenido que no es imagen: la fila queda en Error con detalle, sin tirar el worker
        public async Task Contenido_invalido_queda_en_error()
        {
            using var client = await CreateTenantClientAsync();
            var (grupoId, participanteId) = await CrearGrupoConParticipanteAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoId, participanteId, ("no-es-foto.jpg", "esto no es un jpg"u8.ToArray())));
            await resp.ShouldBeOk(); // el upload acepta; la validación real la hace el pipeline

            var dto = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
            Assert.Equal(EstadoProcesamientoFoto.Error, await EsperarProcesamientoAsync(dto.Id));

            var error = await Factory.QueryScalarAsync<string>(
                $"SELECT ErrorProcesamiento FROM fot_Fotos WHERE Id = {dto.Id}");
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact] // FUP-04 — grupo inexistente (o de otro tenant, es lo mismo por el filtro) → 400 sin filas
        public async Task Grupo_inexistente_da_400()
        {
            using var client = await CreateTenantClientAsync();

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(999999, null, ("a.jpg", CrearJpeg())));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Fotos"));
        }

        [Fact] // FUP-05 — el grupo de OTRO tenant no existe para este (aislamiento en el upload)
        public async Task Grupo_de_otro_tenant_da_400()
        {
            using var tenant3 = await CreateTenantClientAsync(3);
            var (grupoAjeno, _) = await CrearGrupoConParticipanteAsync(tenant3);

            using var tenant2 = await CreateTenantClientAsync(2);
            var resp = await tenant2.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoAjeno, null, ("a.jpg", CrearJpeg())));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Fotos"));
        }

        [Fact] // FUP-06 — participanteId que no pertenece al grupo → 400
        public async Task Participante_de_otro_grupo_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var (grupoA, _) = await CrearGrupoConParticipanteAsync(client);
            var (_, participanteDeOtroGrupo) = await CrearGrupoConParticipanteAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoA, participanteDeOtroGrupo, ("a.jpg", CrearJpeg())));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Fotos"));
        }

        [Fact] // FUP-07 — sin archivos → 400
        public async Task Sin_archivos_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var (grupoId, _) = await CrearGrupoConParticipanteAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload", Multipart(grupoId, null));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
        }

        [Fact] // FUP-08 — listado admin: por grupo trae todo; por participante solo las del participante
        public async Task Listado_filtra_por_grupo_y_participante()
        {
            using var client = await CreateTenantClientAsync();
            var (grupoId, participanteId) = await CrearGrupoConParticipanteAsync(client);

            await (await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoId, participanteId, ("individual.jpg", CrearJpeg())))).ShouldBeOk();
            await (await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(grupoId, null, ("grupal.jpg", CrearJpeg())))).ShouldBeOk();

            var delGrupo = await (await client.GetAsync($"/api/fotos/fotos?grupoId={grupoId}"))
                .Content.ReadFromJsonAsync<List<FotoDto>>();
            Assert.Equal(2, delGrupo!.Count);

            var delParticipante = await (await client.GetAsync($"/api/fotos/fotos?grupoId={grupoId}&participanteId={participanteId}"))
                .Content.ReadFromJsonAsync<List<FotoDto>>();
            var unica = Assert.Single(delParticipante!);
            Assert.Equal("individual.jpg", unica.NombreArchivoOriginal);
        }
    }
}
