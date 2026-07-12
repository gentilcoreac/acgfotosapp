using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Cursos — CRUD Extended con los álbumes (alumnos) como colección hija reconciliada por Id en
    /// SyncCollections. Al dar de alta un álbum el sistema le genera su código de acceso (Fase 1:
    /// "generación de código de acceso al crear álbum"). Guards propios: el EventoId del input debe
    /// existir EN EL TENANT (la FK sola aceptaría un evento ajeno) y no se borran cursos/álbumes
    /// con fotos (la FK Restrict daría 500; acá corta antes con 400). Mismo esquema de tenant que
    /// EventoCrudTests: root + <c>SimulatedTenant</c>.
    /// </summary>
    public class CursoCrudTests : IntegrationTestBase
    {
        private static readonly Regex FormatoCodigo =
            new("^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}$");

        public CursoCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<long> CreateEventoAsync(HttpClient client, string nombre = "Egresados 2026")
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                new { id = 0, nombre, estado = 0, tamanosPrecios = Array.Empty<object>() });
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<EventoDto>())!.Id;
        }

        private static object Input(long eventoId, string nombre, long id = 0, object[]? albumes = null) => new
        {
            id,
            eventoId,
            nombre,
            albumes = albumes ?? Array.Empty<object>(),
        };

        private static object Album(string nombreAlumno, long id = 0) => new { id, nombreAlumno };

        private static async Task<CursoDto> CreateCursoAsync(HttpClient client, long eventoId, string nombre,
            params object[] albumes)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/cursos/update",
                Input(eventoId, nombre, albumes: albumes));
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<CursoDto>())!;
        }

        /// <summary>Arrange de los guards de borrado: inserta una foto del curso/álbum por SQL.</summary>
        private Task InsertFotoAsync(long eventoId, long cursoId, long? albumId, long tenantId = 2) =>
            Factory.ExecuteSqlAsync($@"
                INSERT INTO fot_Fotos (TenantId, EventoId, CursoId, AlbumId, StorageKey,
                                       NombreArchivoOriginal, Ancho, Alto, TamanoBytes,
                                       EstadoProcesamiento, CreadoEn)
                VALUES ({tenantId}, {eventoId}, {cursoId}, {(albumId?.ToString() ?? "NULL")}, NEWID(),
                        'foto.jpg', 100, 100, 1000, 0, GETUTCDATE())");

        [Fact] // CUR-01 — alta happy: persiste curso + álbumes con tenant y genera un código activo por álbum
        public async Task Alta_persiste_curso_con_albumes_y_genera_codigos()
        {
            using var client = await CreateTenantClientAsync(); // tenant 2
            var eventoId = await CreateEventoAsync(client);

            var dto = await CreateCursoAsync(client, eventoId, "7ºB",
                Album("Ana Pérez"), Album("Bruno Díaz"));

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Cursos WHERE Id = {dto.Id} AND TenantId = 2"));
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM fot_Albumes WHERE CursoId = {dto.Id} AND TenantId = 2"));
            Assert.Equal(2, await CountAsync(
                $@"SELECT COUNT(*) FROM fot_CodigosAcceso ca
                   JOIN fot_Albumes a ON a.Id = ca.AlbumId
                   WHERE a.CursoId = {dto.Id} AND ca.Activo = 1 AND ca.TenantId = 2"));

            Assert.Equal(2, dto.Albumes.Count);
            Assert.All(dto.Albumes, a => Assert.Matches(FormatoCodigo, a.CodigoAcceso!));
        }

        [Fact] // CUR-02 — los códigos generados no se repiten entre álbumes
        public async Task Codigos_generados_son_distintos()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);

            var dto = await CreateCursoAsync(client, eventoId, "Numeroso",
                Enumerable.Range(1, 20).Select(i => Album($"Alumno {i:00}")).ToArray<object>());

            var codigos = dto.Albumes.Select(a => a.CodigoAcceso).ToList();
            Assert.Equal(20, codigos.Distinct().Count());
        }

        [Fact] // CUR-03 — detalle: álbumes ordenados por nombre, cada uno con su código
        public async Task Detalle_trae_albumes_ordenados_con_codigo()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateCursoAsync(client, eventoId, "7ºA",
                Album("Zoe Vega"), Album("Ana Pérez"));

            var resp = await client.GetAsync($"/api/fotos/cursos/{creado.Id}");
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<CursoDto>();
            Assert.Equal("7ºA", dto!.Nombre);
            Assert.Equal(eventoId, dto.EventoId);
            Assert.Equal(new[] { "Ana Pérez", "Zoe Vega" }, dto.Albumes.Select(a => a.NombreAlumno));
            Assert.All(dto.Albumes, a => Assert.Matches(FormatoCodigo, a.CodigoAcceso!));
        }

        [Fact] // CUR-04 — listado: filtra por evento y por searchText; expone CantidadAlbumes
        public async Task Listado_filtra_por_evento_y_texto()
        {
            using var client = await CreateTenantClientAsync();
            var evento1 = await CreateEventoAsync(client, "Primaria");
            var evento2 = await CreateEventoAsync(client, "Jardín");
            await CreateCursoAsync(client, evento1, "7ºA", Album("Ana"), Album("Bruno"));
            await CreateCursoAsync(client, evento1, "7ºB");
            await CreateCursoAsync(client, evento2, "Sala Verde");

            var porEvento = await (await client.GetAsync($"/api/fotos/cursos?eventoId={evento1}"))
                .Content.ReadFromJsonAsync<Page<CursoHeaderDto>>();
            Assert.Equal(2, porEvento!.Items.Count);
            Assert.All(porEvento.Items, c => Assert.Equal(evento1, c.EventoId));
            Assert.Equal(2, porEvento.Items.Single(c => c.Nombre == "7ºA").CantidadAlbumes);

            var porTexto = await (await client.GetAsync($"/api/fotos/cursos?eventoId={evento1}&searchText=7ºB"))
                .Content.ReadFromJsonAsync<Page<CursoHeaderDto>>();
            var unico = Assert.Single(porTexto!.Items);
            Assert.Equal("7ºB", unico.Nombre);
        }

        [Fact] // CUR-05 — edición: reconcilia álbumes (update/baja/alta); el que queda conserva su código
        public async Task Edicion_reconcilia_albumes_y_conserva_codigos()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateCursoAsync(client, eventoId, "Original",
                Album("Ana Pérez"), Album("Bruno Díaz"));

            var ana = creado.Albumes.Single(a => a.NombreAlumno == "Ana Pérez");
            var bruno = creado.Albumes.Single(a => a.NombreAlumno == "Bruno Díaz");

            // Se corrige el nombre de Ana, se da de baja a Bruno y se agrega a Carla.
            var resp = await client.PostAsJsonAsync("/api/fotos/cursos/update",
                Input(eventoId, "Renombrado", id: creado.Id, albumes: new[]
                {
                    Album("Ana María Pérez", id: ana.Id),
                    Album("Carla Ruiz"),
                }));
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<CursoDto>();
            Assert.Equal("Renombrado", dto!.Nombre);
            Assert.Equal(2, dto.Albumes.Count);

            var anaEditada = dto.Albumes.Single(a => a.Id == ana.Id);
            Assert.Equal("Ana María Pérez", anaEditada.NombreAlumno);
            Assert.Equal(ana.CodigoAcceso, anaEditada.CodigoAcceso); // el código no se pisa

            var carla = dto.Albumes.Single(a => a.NombreAlumno == "Carla Ruiz");
            Assert.Matches(FormatoCodigo, carla.CodigoAcceso!);

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Albumes WHERE Id = {bruno.Id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_CodigosAcceso WHERE AlbumId = {bruno.Id}"));
        }

        [Fact] // CUR-06 — validación de forma: nombre vacío, alumno vacío y evento 0 → 400
        public async Task Validaciones_de_forma_dan_400()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);

            var sinNombre = await client.PostAsJsonAsync("/api/fotos/cursos/update", Input(eventoId, ""));
            await sinNombre.ShouldBeStatus(HttpStatusCode.BadRequest);

            var alumnoVacio = await client.PostAsJsonAsync("/api/fotos/cursos/update",
                Input(eventoId, "7ºA", albumes: new[] { Album("") }));
            await alumnoVacio.ShouldBeStatus(HttpStatusCode.BadRequest);

            var sinEvento = await client.PostAsJsonAsync("/api/fotos/cursos/update", Input(0, "7ºA"));
            await sinEvento.ShouldBeStatus(HttpStatusCode.BadRequest);

            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Cursos"));
        }

        [Fact] // CUR-07 — guard multi-tenant del input: no se puede colgar un curso de un evento AJENO
        public async Task Evento_de_otro_tenant_da_400()
        {
            using var tenant3 = await CreateTenantClientAsync(3);
            var eventoAjeno = await CreateEventoAsync(tenant3, "De tenant 3");

            using var tenant2 = await CreateTenantClientAsync(2);
            var resp = await tenant2.PostAsJsonAsync("/api/fotos/cursos/update",
                Input(eventoAjeno, "Intruso"));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Cursos"));
        }

        [Fact] // CUR-08 — aislamiento multi-tenant: el curso de tenant 2 no existe para tenant 3
        public async Task Curso_de_otro_tenant_es_invisible()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var eventoId = await CreateEventoAsync(tenant2);
            var creado = await CreateCursoAsync(tenant2, eventoId, "Solo tenant 2", Album("Ana"));

            using var tenant3 = await CreateTenantClientAsync(3);

            var detalle = await tenant3.GetAsync($"/api/fotos/cursos/{creado.Id}");
            await detalle.ShouldBeStatus(HttpStatusCode.NotFound);

            var page = await (await tenant3.GetAsync("/api/fotos/cursos"))
                .Content.ReadFromJsonAsync<Page<CursoHeaderDto>>();
            Assert.Empty(page!.Items);
        }

        [Fact] // CUR-09 — delete: cascadea álbumes y códigos de acceso
        public async Task Delete_cascadea_albumes_y_codigos()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateCursoAsync(client, eventoId, "Descartable", Album("Ana"), Album("Bruno"));

            var resp = await client.DeleteAsync($"/api/fotos/cursos/{creado.Id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Cursos WHERE Id = {creado.Id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Albumes WHERE CursoId = {creado.Id}"));
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_CodigosAcceso"));
        }

        [Fact] // CUR-10 — guard: un curso con fotos no se borra (400, sin tocar datos)
        public async Task Delete_de_curso_con_fotos_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateCursoAsync(client, eventoId, "Con fotos", Album("Ana"));
            await InsertFotoAsync(eventoId, creado.Id, albumId: null); // grupal del curso

            var resp = await client.DeleteAsync($"/api/fotos/cursos/{creado.Id}");

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Cursos WHERE Id = {creado.Id}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Albumes WHERE CursoId = {creado.Id}"));
        }

        [Fact] // CUR-11 — guard: la edición no puede dar de baja un álbum que tiene fotos
        public async Task Baja_de_album_con_fotos_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateCursoAsync(client, eventoId, "7ºA", Album("Ana"), Album("Bruno"));
            var ana = creado.Albumes.Single(a => a.NombreAlumno == "Ana");
            await InsertFotoAsync(eventoId, creado.Id, albumId: ana.Id);

            // Se intenta dejar solo a Bruno (baja de Ana, que tiene una foto).
            var bruno = creado.Albumes.Single(a => a.NombreAlumno == "Bruno");
            var resp = await client.PostAsJsonAsync("/api/fotos/cursos/update",
                Input(eventoId, "7ºA", id: creado.Id, albumes: new[] { Album("Bruno", id: bruno.Id) }));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM fot_Albumes WHERE CursoId = {creado.Id}"));
        }
    }
}
