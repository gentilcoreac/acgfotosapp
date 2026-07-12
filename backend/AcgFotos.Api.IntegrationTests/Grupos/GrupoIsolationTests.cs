using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Grupos
{
    /// <summary>
    /// Grupos (220) — BASELINE de aislamiento multi-tenant en LECTURA (lista + get-by-id). Es el test
    /// más básico de la entidad (tenant A no ve grupos de B). El create/guard ya está en MultiTenant*
    /// (MT-05/06/07) → acá solo lo de lectura, que faltaba. Los grupos se crean vía API: adminb2 en el
    /// tenant 2 y adminb en el tenant 3 (admin ignora el tenant inactivo, AUTH-13).
    /// </summary>
    public class GrupoIsolationTests : IntegrationTestBase
    {
        public GrupoIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record GrupoRow(string Nombre);
        private sealed record Paginated(IReadOnlyList<GrupoRow> Items, int TotalCount);

        private async Task CreateGrupoAsync(string actor, string nombre)
        {
            using var client = await CreateAuthenticatedClientAsync(actor);
            var resp = await client.PostAsJsonAsync("/api/general/grupos/update", Payloads.Grupo(nombre));
            await resp.ShouldBeOk();
        }

        [Fact] // el listado de grupos solo trae los del tenant del contexto
        public async Task Listado_de_grupos_solo_del_tenant()
        {
            await CreateGrupoAsync(TestData.AdminB2, "g-t2"); // tenant 2
            await CreateGrupoAsync(TestData.AdminB, "g-t3");  // tenant 3 (admin ignora inactivo)

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.GetAsync("/api/general/grupos");

            await resp.ShouldBeStatus(System.Net.HttpStatusCode.OK);
            var page = await resp.Content.ReadFromJsonAsync<Paginated>();
            var names = page!.Items.Select(i => i.Nombre).ToList();
            Assert.Contains("g-t2", names);
            Assert.DoesNotContain("g-t3", names); // no se filtra el grupo de otro tenant
        }

        [Fact] // no se puede leer por Id un grupo de otro tenant
        public async Task Get_grupo_de_otro_tenant_no_expone()
        {
            await CreateGrupoAsync(TestData.AdminB, "g-t3-secreto"); // tenant 3
            var id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Grupos WHERE Nombre = 'g-t3-secreto'");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.GetAsync($"/api/general/grupos/{id}");

            // AISLAMIENTO + manejo de error: grupo de otro tenant -> filtrado -> null -> 404 (no 500 ni datos).
            // HALLAZGO #17 CORREGIDO (mismo patrón que #9 en Usuario): GrupoAppService.GetByIdAsync ahora
            // hace null-check antes de mapear/enriquecer.
            await resp.ShouldBeStatus(System.Net.HttpStatusCode.NotFound);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.DoesNotContain("g-t3-secreto", body);
        }
    }
}
