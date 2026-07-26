using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Grupos — CRUD Extended con los participantes (participantes) como colección hija reconciliada por Id en
    /// SyncCollections. Al dar de alta un participante el sistema le genera su código de acceso (Fase 1:
    /// "generación de código de acceso al crear participante"). Guards propios: el EventoId del input debe
    /// existir EN EL TENANT (la FK sola aceptaría un evento ajeno) y no se borran grupos/participantes
    /// con fotos (la FK Restrict daría 500; acá corta antes con 400). Mismo esquema de tenant que
    /// EventoCrudTests: root + <c>SimulatedTenant</c>.
    /// </summary>
    public class GrupoCrudTests : IntegrationTestBase
    {
        private static readonly Regex FormatoCodigo =
            new("^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}$");

        public GrupoCrudTests(TestWebApplicationFactory factory) : base(factory) { }

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

        private static object Input(long eventoId, string nombre, long id = 0, object[]? participantes = null) => new
        {
            id,
            eventoId,
            nombre,
            participantes = participantes ?? Array.Empty<object>(),
        };

        private static object Participante(string nombre, long id = 0) => new { id, nombre };

        private static async Task<GrupoDto> CreateGrupoAsync(HttpClient client, long eventoId, string nombre,
            params object[] participantes)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/grupos/update",
                Input(eventoId, nombre, participantes: participantes));
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<GrupoDto>())!;
        }

        /// <summary>Arrange de los guards de borrado: inserta una foto del grupo/participante por SQL.</summary>
        private Task InsertFotoAsync(long eventoId, long grupoId, long? participanteId, long tenantId = 2) =>
            Factory.ExecuteSqlAsync($@"
                INSERT INTO fot_Fotos (TenantId, EventoId, GrupoId, ParticipanteId, StorageKey,
                                       NombreArchivoOriginal, Ancho, Alto, TamanoBytes,
                                       EstadoProcesamiento, CreadoEn)
                VALUES ({tenantId}, {eventoId}, {grupoId}, {(participanteId?.ToString() ?? "NULL")}, gen_random_uuid(),
                        'foto.jpg', 100, 100, 1000, 0, now())");

        [Fact] // CUR-01 — alta happy: persiste grupo + participantes con tenant y genera un código activo por participante
        public async Task Alta_persiste_grupo_con_participantes_y_genera_codigos()
        {
            using var client = await CreateTenantClientAsync(); // tenant 2
            var eventoId = await CreateEventoAsync(client);

            var dto = await CreateGrupoAsync(client, eventoId, "7ºB",
                Participante("Ana Pérez"), Participante("Bruno Díaz"));

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Grupos WHERE Id = {dto.Id} AND TenantId = 2"));
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM fot_Participantes WHERE GrupoId = {dto.Id} AND TenantId = 2"));
            Assert.Equal(2, await CountAsync(
                $@"SELECT COUNT(*) FROM fot_CodigosAcceso ca
                   JOIN fot_Participantes a ON a.Id = ca.ParticipanteId
                   WHERE a.GrupoId = {dto.Id} AND ca.Activo = true AND ca.TenantId = 2"));

            Assert.Equal(2, dto.Participantes.Count);
            Assert.All(dto.Participantes, a => Assert.Matches(FormatoCodigo, a.CodigoAcceso!));
        }

        [Fact] // CUR-02 — los códigos generados no se repiten entre participantes
        public async Task Codigos_generados_son_distintos()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);

            var dto = await CreateGrupoAsync(client, eventoId, "Numeroso",
                Enumerable.Range(1, 20).Select(i => Participante($"Participante {i:00}")).ToArray<object>());

            var codigos = dto.Participantes.Select(a => a.CodigoAcceso).ToList();
            Assert.Equal(20, codigos.Distinct().Count());
        }

        [Fact] // CUR-03 — detalle: participantes ordenados por nombre, cada uno con su código
        public async Task Detalle_trae_participantes_ordenados_con_codigo()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateGrupoAsync(client, eventoId, "7ºA",
                Participante("Zoe Vega"), Participante("Ana Pérez"));

            var resp = await client.GetAsync($"/api/fotos/grupos/{creado.Id}");
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<GrupoDto>();
            Assert.Equal("7ºA", dto!.Nombre);
            Assert.Equal(eventoId, dto.EventoId);
            Assert.Equal(new[] { "Ana Pérez", "Zoe Vega" }, dto.Participantes.Select(a => a.Nombre));
            Assert.All(dto.Participantes, a => Assert.Matches(FormatoCodigo, a.CodigoAcceso!));
        }

        [Fact] // CUR-04 — listado: filtra por evento y por searchText; expone CantidadParticipantes
        public async Task Listado_filtra_por_evento_y_texto()
        {
            using var client = await CreateTenantClientAsync();
            var evento1 = await CreateEventoAsync(client, "Primaria");
            var evento2 = await CreateEventoAsync(client, "Jardín");
            await CreateGrupoAsync(client, evento1, "7ºA", Participante("Ana"), Participante("Bruno"));
            await CreateGrupoAsync(client, evento1, "7ºB");
            await CreateGrupoAsync(client, evento2, "Sala Verde");

            var porEvento = await (await client.GetAsync($"/api/fotos/grupos?eventoId={evento1}"))
                .Content.ReadFromJsonAsync<Page<GrupoHeaderDto>>();
            Assert.Equal(2, porEvento!.Items.Count);
            Assert.All(porEvento.Items, c => Assert.Equal(evento1, c.EventoId));
            Assert.Equal(2, porEvento.Items.Single(c => c.Nombre == "7ºA").CantidadParticipantes);

            var porTexto = await (await client.GetAsync($"/api/fotos/grupos?eventoId={evento1}&searchText=7ºB"))
                .Content.ReadFromJsonAsync<Page<GrupoHeaderDto>>();
            var unico = Assert.Single(porTexto!.Items);
            Assert.Equal("7ºB", unico.Nombre);
        }

        [Fact] // CUR-05 — edición: reconcilia participantes (update/baja/alta); el que queda conserva su código
        public async Task Edicion_reconcilia_participantes_y_conserva_codigos()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateGrupoAsync(client, eventoId, "Original",
                Participante("Ana Pérez"), Participante("Bruno Díaz"));

            var ana = creado.Participantes.Single(a => a.Nombre == "Ana Pérez");
            var bruno = creado.Participantes.Single(a => a.Nombre == "Bruno Díaz");

            // Se corrige el nombre de Ana, se da de baja a Bruno y se agrega a Carla.
            var resp = await client.PostAsJsonAsync("/api/fotos/grupos/update",
                Input(eventoId, "Renombrado", id: creado.Id, participantes: new[]
                {
                    Participante("Ana María Pérez", id: ana.Id),
                    Participante("Carla Ruiz"),
                }));
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<GrupoDto>();
            Assert.Equal("Renombrado", dto!.Nombre);
            Assert.Equal(2, dto.Participantes.Count);

            var anaEditada = dto.Participantes.Single(a => a.Id == ana.Id);
            Assert.Equal("Ana María Pérez", anaEditada.Nombre);
            Assert.Equal(ana.CodigoAcceso, anaEditada.CodigoAcceso); // el código no se pisa

            var carla = dto.Participantes.Single(a => a.Nombre == "Carla Ruiz");
            Assert.Matches(FormatoCodigo, carla.CodigoAcceso!);

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Participantes WHERE Id = {bruno.Id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_CodigosAcceso WHERE ParticipanteId = {bruno.Id}"));
        }

        [Fact] // CUR-06 — validación de forma: nombre vacío, participante vacío y evento 0 → 400
        public async Task Validaciones_de_forma_dan_400()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);

            var sinNombre = await client.PostAsJsonAsync("/api/fotos/grupos/update", Input(eventoId, ""));
            await sinNombre.ShouldBeStatus(HttpStatusCode.BadRequest);

            var participanteVacio = await client.PostAsJsonAsync("/api/fotos/grupos/update",
                Input(eventoId, "7ºA", participantes: new[] { Participante("") }));
            await participanteVacio.ShouldBeStatus(HttpStatusCode.BadRequest);

            var sinEvento = await client.PostAsJsonAsync("/api/fotos/grupos/update", Input(0, "7ºA"));
            await sinEvento.ShouldBeStatus(HttpStatusCode.BadRequest);

            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Grupos"));
        }

        [Fact] // CUR-07 — guard multi-tenant del input: no se puede colgar un grupo de un evento AJENO
        public async Task Evento_de_otro_tenant_da_400()
        {
            using var tenant3 = await CreateTenantClientAsync(3);
            var eventoAjeno = await CreateEventoAsync(tenant3, "De tenant 3");

            using var tenant2 = await CreateTenantClientAsync(2);
            var resp = await tenant2.PostAsJsonAsync("/api/fotos/grupos/update",
                Input(eventoAjeno, "Intruso"));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Grupos"));
        }

        [Fact] // CUR-08 — aislamiento multi-tenant: el grupo de tenant 2 no existe para tenant 3
        public async Task Grupo_de_otro_tenant_es_invisible()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var eventoId = await CreateEventoAsync(tenant2);
            var creado = await CreateGrupoAsync(tenant2, eventoId, "Solo tenant 2", Participante("Ana"));

            using var tenant3 = await CreateTenantClientAsync(3);

            var detalle = await tenant3.GetAsync($"/api/fotos/grupos/{creado.Id}");
            await detalle.ShouldBeStatus(HttpStatusCode.NotFound);

            var page = await (await tenant3.GetAsync("/api/fotos/grupos"))
                .Content.ReadFromJsonAsync<Page<GrupoHeaderDto>>();
            Assert.Empty(page!.Items);
        }

        [Fact] // CUR-09 — delete: cascadea participantes y códigos de acceso
        public async Task Delete_cascadea_participantes_y_codigos()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateGrupoAsync(client, eventoId, "Descartable", Participante("Ana"), Participante("Bruno"));

            var resp = await client.DeleteAsync($"/api/fotos/grupos/{creado.Id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Grupos WHERE Id = {creado.Id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Participantes WHERE GrupoId = {creado.Id}"));
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_CodigosAcceso"));
        }

        [Fact] // CUR-10 — guard: un grupo con fotos no se borra (400, sin tocar datos)
        public async Task Delete_de_grupo_con_fotos_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateGrupoAsync(client, eventoId, "Con fotos", Participante("Ana"));
            await InsertFotoAsync(eventoId, creado.Id, participanteId: null); // grupal del grupo

            var resp = await client.DeleteAsync($"/api/fotos/grupos/{creado.Id}");

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Grupos WHERE Id = {creado.Id}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Participantes WHERE GrupoId = {creado.Id}"));
        }

        [Fact] // CUR-11 — guard: la edición no puede dar de baja un participante que tiene fotos
        public async Task Baja_de_participante_con_fotos_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var eventoId = await CreateEventoAsync(client);
            var creado = await CreateGrupoAsync(client, eventoId, "7ºA", Participante("Ana"), Participante("Bruno"));
            var ana = creado.Participantes.Single(a => a.Nombre == "Ana");
            await InsertFotoAsync(eventoId, creado.Id, participanteId: ana.Id);

            // Se intenta dejar solo a Bruno (baja de Ana, que tiene una foto).
            var bruno = creado.Participantes.Single(a => a.Nombre == "Bruno");
            var resp = await client.PostAsJsonAsync("/api/fotos/grupos/update",
                Input(eventoId, "7ºA", id: creado.Id, participantes: new[] { Participante("Bruno", id: bruno.Id) }));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM fot_Participantes WHERE GrupoId = {creado.Id}"));
        }
    }
}
