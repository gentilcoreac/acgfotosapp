using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Permisos (170) — la regla central de visibilidad: EsRestringido. ShouldExcludeRestringidos() es
    /// `!(IsRoot && TenantId==RootTenantId)` → root-en-su-contexto los ve; un no-root NO. Se aplica al
    /// listado, al GetAll y al árbol jerárquico, PERO **no** al detalle por id (gotcha PER-15). El flag se
    /// cambia por endpoint dedicado solo-root (guarda IsRoot server-side). Permiso es catálogo global → no
    /// hay filtro por tenant; la única segregación es este flag.
    /// Seed: Permiso 1 (PermisoRoot, EsRestringido=1). Un permiso visible se siembra inline.
    /// </summary>
    public class PermisoRestringidoTests : IntegrationTestBase
    {
        public PermisoRestringidoTests(TestWebApplicationFactory factory) : base(factory) { }

        // Siembra un permiso visible (EsRestringido=0) y devuelve su Id.
        private async Task<long> SeedVisibleAsync(string codigo)
        {
            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Permisos (Nombre, CodigoPermiso, Descripcion, Activo, PermisoPadreId, AplicacionId, EsRestringido) " +
                $"VALUES ('{codigo}', '{codigo}', '{codigo}', true, NULL, 1, false)");
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Permisos WHERE CodigoPermiso = '{codigo}'");
        }

        private async Task<Page<PermisoHeaderDto>> ListAsync(string actor)
        {
            using var client = await CreateAuthenticatedClientAsync(actor);
            var resp = await client.GetAsync("/api/general/permisos");
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<Page<PermisoHeaderDto>>())!;
        }

        [Fact] // PER-01 — listado como root incluye los restringidos (Permiso 1 EsRestringido=1)
        public async Task Listado_root_incluye_restringidos()
        {
            await SeedVisibleAsync("qa_visible");

            var page = await ListAsync(TestData.Root);

            Assert.Contains(page.Items, p => p.Id == 1);                     // restringido visible para root
            Assert.Contains(page.Items, p => p.CodigoPermiso == "qa_visible");
        }

        [Fact] // PER-02/61 — listado como no-root EXCLUYE los restringidos; sí ve los visibles (catálogo global)
        public async Task Listado_no_root_excluye_restringidos()
        {
            await SeedVisibleAsync("qa_visible");

            var page = await ListAsync(TestData.UserB); // no-root, tenant 2

            Assert.DoesNotContain(page.Items, p => p.Id == 1);              // Permiso 1 (restringido) oculto
            Assert.Contains(page.Items, p => p.CodigoPermiso == "qa_visible"); // el visible sí (sin filtro por tenant)
        }

        [Fact] // PER-15 — GOTCHA: el detalle por id NO aplica excludeRestringidos → un no-root lee el detalle
               // de un permiso restringido por acceso directo (inconsistente con listado/árbol). Documentado.
        public async Task Detalle_por_id_no_filtra_restringidos()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.GetAsync("/api/general/permisos/1"); // Permiso 1 es restringido
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<PermisoDto>();
            Assert.Equal("PermisoRoot", dto!.CodigoPermiso); // expuesto pese a ser restringido (GetByIdWithDetailsAsync no filtra)
        }

        [Fact] // PER-43 — árbol jerárquico como root incluye los restringidos
        public async Task Arbol_root_incluye_restringidos()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.GetAsync("/api/general/permisos/hierarchical-items");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("PermisoRoot", raw);
        }

        [Fact] // PER-44 — árbol jerárquico como no-root EXCLUYE los restringidos
        public async Task Arbol_no_root_excluye_restringidos()
        {
            await SeedVisibleAsync("qa_arbol");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root
            var resp = await client.GetAsync("/api/general/permisos/hierarchical-items");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.DoesNotContain("PermisoRoot", raw); // el restringido no está en el árbol del no-root
            Assert.Contains("qa_arbol", raw);          // el visible sí
        }

        [Fact] // PER-48 — set-es-restringido: root activa el flag
        public async Task SetEsRestringido_root_activa()
        {
            long id = await SeedVisibleAsync("qa_set_on");

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync($"/api/general/permisos/{id}/set-es-restringido",
                new { esRestringido = true });
            await resp.ShouldBeOk();

            Assert.Equal(1, await Factory.QueryScalarAsync<int>($"SELECT CAST(EsRestringido AS int) FROM gen_Permisos WHERE Id = {id}"));
        }

        [Fact] // PER-49 — set-es-restringido: root desactiva el flag (Permiso 1, arranca restringido)
        public async Task SetEsRestringido_root_desactiva()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/permisos/1/set-es-restringido",
                new { esRestringido = false });
            await resp.ShouldBeOk();

            Assert.Equal(0, await Factory.QueryScalarAsync<int>("SELECT CAST(EsRestringido AS int) FROM gen_Permisos WHERE Id = 1"));
        }

        [Fact] // PER-50 — set-es-restringido: no-root rechazado server-side (BusinessValidationException); flag intacto
        public async Task SetEsRestringido_no_root_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root
            var resp = await client.PostAsJsonAsync("/api/general/permisos/1/set-es-restringido",
                new { esRestringido = false });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            Assert.Equal(1, await Factory.QueryScalarAsync<int>("SELECT CAST(EsRestringido AS int) FROM gen_Permisos WHERE Id = 1")); // sigue restringido
        }

        [Fact] // PER-54 — set-es-restringido: id inexistente → BusinessValidationException, no 500 crudo
        public async Task SetEsRestringido_id_inexistente_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/permisos/999999/set-es-restringido",
                new { esRestringido = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorGenericInternal);
        }
    }
}
