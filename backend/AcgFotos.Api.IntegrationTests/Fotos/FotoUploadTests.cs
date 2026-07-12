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

        /// <summary>Arma evento + curso (+ álbum "Ana") y devuelve (cursoId, albumId).</summary>
        private static async Task<(long CursoId, long AlbumId)> CrearCursoConAlbumAsync(HttpClient client)
        {
            var evento = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                new { id = 0, nombre = "Egresados 2026", estado = 0, tamanosPrecios = Array.Empty<object>() });
            await evento.ShouldBeOk();
            var eventoId = (await evento.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var curso = await client.PostAsJsonAsync("/api/fotos/cursos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºA",
                albumes = new[] { new { id = 0, nombreAlumno = "Ana" } },
            });
            await curso.ShouldBeOk();
            var cursoDto = (await curso.Content.ReadFromJsonAsync<CursoDto>())!;

            return (cursoDto.Id, cursoDto.Albumes.Single().Id);
        }

        private static byte[] CrearJpeg(int ancho = 1600, int alto = 1200)
        {
            using var imagen = new Image<Rgb24>(ancho, alto, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private static MultipartFormDataContent Multipart(long cursoId, long? albumId,
            params (string Nombre, byte[] Contenido)[] archivos)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(cursoId.ToString()), "cursoId" },
            };
            if (albumId is not null)
            {
                content.Add(new StringContent(albumId.ToString()!), "albumId");
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

        [Fact] // FUP-01 — happy: sube 2 al álbum, quedan Pendiente y el worker las deja Lista con derivados
        public async Task Upload_a_album_procesa_y_deja_derivados()
        {
            using var client = await CreateTenantClientAsync(); // tenant 2
            var (cursoId, albumId) = await CrearCursoConAlbumAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoId, albumId, ("ana-01.jpg", CrearJpeg()), ("ana-02.jpg", CrearJpeg(800, 600))));
            await resp.ShouldBeOk();

            var dtos = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!;
            Assert.Equal(2, dtos.Count);
            Assert.All(dtos, f => Assert.Equal(albumId, f.AlbumId));

            Assert.Equal(2, await CountAsync(
                $"SELECT COUNT(*) FROM fot_Fotos WHERE CursoId = {cursoId} AND TenantId = 2"));

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
            Assert.True(File.Exists(RutaStorage($"fotos/derived/{eventoId}/{storageKey:N}_thumb.jpg")), "falta el thumb");
            Assert.True(File.Exists(RutaStorage($"fotos/derived/{eventoId}/{storageKey:N}_preview.jpg")), "falta el preview");
        }

        [Fact] // FUP-02 — upload sin albumId: foto GRUPAL del curso (AlbumId null)
        public async Task Upload_sin_album_es_grupal()
        {
            using var client = await CreateTenantClientAsync();
            var (cursoId, _) = await CrearCursoConAlbumAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoId, albumId: null, ("grupal.jpg", CrearJpeg())));
            await resp.ShouldBeOk();

            var dto = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
            Assert.Null(dto.AlbumId);
            Assert.Equal(1, await CountAsync(
                $"SELECT COUNT(*) FROM fot_Fotos WHERE Id = {dto.Id} AND AlbumId IS NULL"));
        }

        [Fact] // FUP-03 — contenido que no es imagen: la fila queda en Error con detalle, sin tirar el worker
        public async Task Contenido_invalido_queda_en_error()
        {
            using var client = await CreateTenantClientAsync();
            var (cursoId, albumId) = await CrearCursoConAlbumAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoId, albumId, ("no-es-foto.jpg", "esto no es un jpg"u8.ToArray())));
            await resp.ShouldBeOk(); // el upload acepta; la validación real la hace el pipeline

            var dto = (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
            Assert.Equal(EstadoProcesamientoFoto.Error, await EsperarProcesamientoAsync(dto.Id));

            var error = await Factory.QueryScalarAsync<string>(
                $"SELECT ErrorProcesamiento FROM fot_Fotos WHERE Id = {dto.Id}");
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact] // FUP-04 — curso inexistente (o de otro tenant, es lo mismo por el filtro) → 400 sin filas
        public async Task Curso_inexistente_da_400()
        {
            using var client = await CreateTenantClientAsync();

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(999999, null, ("a.jpg", CrearJpeg())));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Fotos"));
        }

        [Fact] // FUP-05 — el curso de OTRO tenant no existe para este (aislamiento en el upload)
        public async Task Curso_de_otro_tenant_da_400()
        {
            using var tenant3 = await CreateTenantClientAsync(3);
            var (cursoAjeno, _) = await CrearCursoConAlbumAsync(tenant3);

            using var tenant2 = await CreateTenantClientAsync(2);
            var resp = await tenant2.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoAjeno, null, ("a.jpg", CrearJpeg())));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Fotos"));
        }

        [Fact] // FUP-06 — albumId que no pertenece al curso → 400
        public async Task Album_de_otro_curso_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var (cursoA, _) = await CrearCursoConAlbumAsync(client);
            var (_, albumDeOtroCurso) = await CrearCursoConAlbumAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoA, albumDeOtroCurso, ("a.jpg", CrearJpeg())));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Fotos"));
        }

        [Fact] // FUP-07 — sin archivos → 400
        public async Task Sin_archivos_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var (cursoId, _) = await CrearCursoConAlbumAsync(client);

            var resp = await client.PostAsync("/api/fotos/fotos/upload", Multipart(cursoId, null));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
        }

        [Fact] // FUP-08 — listado admin: por curso trae todo; por álbum solo las del álbum
        public async Task Listado_filtra_por_curso_y_album()
        {
            using var client = await CreateTenantClientAsync();
            var (cursoId, albumId) = await CrearCursoConAlbumAsync(client);

            await (await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoId, albumId, ("individual.jpg", CrearJpeg())))).ShouldBeOk();
            await (await client.PostAsync("/api/fotos/fotos/upload",
                Multipart(cursoId, null, ("grupal.jpg", CrearJpeg())))).ShouldBeOk();

            var delCurso = await (await client.GetAsync($"/api/fotos/fotos?cursoId={cursoId}"))
                .Content.ReadFromJsonAsync<List<FotoDto>>();
            Assert.Equal(2, delCurso!.Count);

            var delAlbum = await (await client.GetAsync($"/api/fotos/fotos?cursoId={cursoId}&albumId={albumId}"))
                .Content.ReadFromJsonAsync<List<FotoDto>>();
            var unica = Assert.Single(delAlbum!);
            Assert.Equal("individual.jpg", unica.NombreArchivoOriginal);
        }
    }
}
