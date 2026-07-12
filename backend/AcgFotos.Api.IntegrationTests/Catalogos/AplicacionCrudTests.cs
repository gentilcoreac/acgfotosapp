using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Aplicaciones (180) — catálogo GLOBAL (Aplicacion : EntityBase → aislamiento MT N/A). CRUD heredado
    /// del base (AplicacionController : EntityApiControllerBase, NO Extended → SyncCollections es no-op:
    /// los Permisos NO se reconcilian desde el write path; viven por Permiso.AplicacionId). Listado proyecta
    /// solo el header (sin Icono/IconoUrl/Permisos). Detalle carga los Permisos aparte (GetPermisosByIdAsync)
    /// y tiene null-guard (id inexistente → 404). Seed: Aplicacion 1 (general) con Permiso 1 (PermisoRoot).
    /// </summary>
    public class AplicacionCrudTests : IntegrationTestBase
    {
        public AplicacionCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<long> CreateAppAsync(HttpClient client, string codigo, string nombre,
            string? icono = null, bool activo = true)
        {
            var resp = await client.PostAsJsonAsync("/api/general/aplicaciones/update",
                Payloads.AplicacionInput(codigo, nombre, icono: icono, activo: activo));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Aplicaciones WHERE Codigo = '{codigo}'");
        }

        [Fact] // APP-01/02 — listado paginado: el header NO trae Icono/IconoUrl ni Permisos (proyección SQL)
        public async Task Listado_proyecta_solo_header()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            await CreateAppAsync(client, "QA-HDR", "QA Header", icono: "home");

            var resp = await client.GetAsync("/api/general/aplicaciones");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<AplicacionDto>>();
            var item = Assert.Single(page!.Items, a => a.Codigo == "QA-HDR");
            Assert.Equal("QA Header", item.Nombre);
            Assert.Null(item.Icono);          // la proyección no trae Icono
            Assert.Empty(item.Permisos);      // ni los permisos
        }

        [Fact] // APP-03 — SearchText filtra en SQL por Codigo o Nombre
        public async Task SearchText_filtra_por_codigo_o_nombre()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateAppAsync(client, "BUDGET", "Budgeting");
            await CreateAppAsync(client, "PBI", "PowerBI");

            var resp = await client.GetAsync("/api/general/aplicaciones?searchText=Budg");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<AplicacionDto>>();
            Assert.Contains(page!.Items, a => a.Codigo == "BUDGET");
            Assert.DoesNotContain(page.Items, a => a.Codigo == "PBI");
        }

        [Fact] // APP-04 — SearchText sin coincidencias: vacío
        public async Task SearchText_sin_coincidencias_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/aplicaciones?searchText=zzzznoexiste");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<AplicacionDto>>();
            Assert.Equal(0, page!.TotalCount);
            Assert.Empty(page.Items);
        }

        [Fact] // APP-16 — alta happy: persiste Codigo/Nombre/Activo/Icono/IconoUrl
        public async Task Alta_persiste_escalares()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/aplicaciones/update",
                Payloads.AplicacionInput("QA", "QA App", icono: "i", iconoUrl: "u", activo: true));
            await resp.ShouldBeOk();

            var id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Aplicaciones WHERE Codigo = 'QA'");
            Assert.True(id > 0);
            Assert.Equal("QA App", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Aplicaciones WHERE Id = {id}"));
            Assert.Equal("i", await Factory.QueryScalarAsync<string>($"SELECT Icono FROM gen_Aplicaciones WHERE Id = {id}"));
        }

        [Fact] // APP-17 — edición: cambia escalares sobre la entidad tracked (SetValues)
        public async Task Edicion_cambia_escalares()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateAppAsync(client, "QA-ED", "Viejo", activo: true);

            var resp = await client.PostAsJsonAsync("/api/general/aplicaciones/update",
                Payloads.AplicacionInput("QA-ED", "Nuevo", id: id, activo: false));
            await resp.ShouldBeOk();

            Assert.Equal("Nuevo", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Aplicaciones WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT CAST(Activo AS int) FROM gen_Aplicaciones WHERE Id = {id}"));
        }

        [Fact] // APP-09 — detalle: trae los Permisos cargados aparte (por Permiso.AplicacionId, no por navegación)
        public async Task Detalle_trae_permisos()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/aplicaciones/1"); // app general, tiene Permiso 1
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<AplicacionDto>();
            Assert.Equal("general", dto!.Codigo);
            Assert.Contains(dto.Permisos, p => p.Id == 1);
        }

        [Fact] // APP-11 — detalle id inexistente: null-guard explícito (entity==null) → 404 (no en GetByIdInexistenteTests)
        public async Task Detalle_id_inexistente_da_404()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/aplicaciones/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // APP-24 — anti mass-assignment: los Permisos del body se IGNORAN (no hay SyncCollections override)
        public async Task Update_con_permisos_en_body_no_los_persiste()
        {
            using var client = await CreateAuthenticatedClientAsync();
            int antes = await CountAsync("SELECT COUNT(*) FROM gen_Permisos WHERE AplicacionId = 1");

            var resp = await client.PostAsJsonAsync("/api/general/aplicaciones/update",
                new { id = 1, codigo = "general", nombre = "General", activo = true,
                      permisos = new[] { new { id = 0, nombre = "FALSO", codigoPermiso = "falso", aplicacionId = 1 } } });
            await resp.ShouldBeOk();

            // El set de permisos de la app 1 no cambia: la colección del DTO no se reconcilia en el write path.
            Assert.Equal(antes, await CountAsync("SELECT COUNT(*) FROM gen_Permisos WHERE AplicacionId = 1"));
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_Permisos WHERE CodigoPermiso = 'falso'"));
        }

        [Fact] // APP-27 — delete happy: app sin referencias se elimina
        public async Task Delete_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateAppAsync(client, "QA-DEL", "Descartable");

            var resp = await client.DeleteAsync($"/api/general/aplicaciones/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_Aplicaciones WHERE Id = {id}"));
        }

        [Fact] // APP-28 — delete de app referenciada por Permiso: comportamiento controlado (no 500 crudo)
        public async Task Delete_app_referenciada_no_revienta()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateAppAsync(client, "QA-REF", "Referenciada");
            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Permisos (Nombre, CodigoPermiso, Descripcion, Activo, PermisoPadreId, AplicacionId, EsRestringido) " +
                $"VALUES (N'PrefApp', N'PrefApp', N'p', 1, NULL, {id}, 0)");

            var resp = await client.DeleteAsync($"/api/general/aplicaciones/{id}");

            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
        }

        [Fact] // APP-58 — idempotencia: reenviar el mismo update no muta la app
        public async Task Reenviar_mismo_update_no_muta()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateAppAsync(client, "QA-IDEM", "Estable", activo: true);

            var resp = await client.PostAsJsonAsync("/api/general/aplicaciones/update",
                Payloads.AplicacionInput("QA-IDEM", "Estable", id: id, activo: true));
            await resp.ShouldBeOk();

            Assert.Equal("Estable", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Aplicaciones WHERE Id = {id}"));
            Assert.Equal(1, await CountAsync($"SELECT CAST(Activo AS int) FROM gen_Aplicaciones WHERE Id = {id}"));
        }
    }
}
