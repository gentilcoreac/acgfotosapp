using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — BASELINE de aislamiento. Tenant NO es IMultiTenantEntityBase: root administra
    /// TODOS los tenants (sin simular) y la proteccion del ABM es por authz de endpoint (root-only). El
    /// UNICO endpoint que toca un NO-root es edit-by-admin-client (un admin autogestiona el branding de SU
    /// tenant). HALLAZGO #13: ese endpoint solo validaba IsAdmin y usaba el Id del body SIN chequear que
    /// fuera el tenant del caller -> un admin del tenant B podia editar el branding del tenant C
    /// (cross-tenant write). FIX: exige Id == AppContext.TenantId. Estos tests verifican el aislamiento.
    /// </summary>
    public class TenantIsolationTests : IntegrationTestBase
    {
        public TenantIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record TenantHeaderRow(long Id, string Codigo);

        [Fact] // HALLAZGO #13 (asegurado) — un admin NO puede editar el branding de OTRO tenant
        public async Task EditByAdminClient_de_otro_tenant_es_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin del tenant 2

            // Intenta editar el tenant 3 (ajeno) mandando su Id en el body.
            var resp = await client.PostAsJsonAsync("/api/general/tenants/edit-by-admin-client",
                new { id = TestData.InactiveTenantId, tituloWeb = "hackeado-por-b2", activo = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            // El branding del tenant 3 NO se toco (el valor malicioso no se persistio).
            var hackeados = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_Tenants WHERE Id = {TestData.InactiveTenantId} AND TituloWeb = 'hackeado-por-b2'");
            Assert.Equal(0, hackeados);
        }

        [Fact] // TEN-56 — edit-by-admin-client rechazado a un usuario NO admin
        public async Task EditByAdminClient_de_no_admin_es_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-admin, tenant 2

            var resp = await client.PostAsJsonAsync("/api/general/tenants/edit-by-admin-client",
                new { id = TestData.ActiveTenantId, tituloWeb = "x", activo = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
        }

        [Fact] // TEN-34 — tenants-for-root con un NO-root no enumera tenants (null -> 204 No Content, sin body)
        public async Task TenantsForRoot_no_root_no_enumera()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.GetAsync("/api/general/tenants/tenants-for-root");

            // El AppService devuelve null para no-root (sin enumeracion); Ok(null) sale como 204 No Content
            // (el catalogo TEN-34 decia "200 con body null"; el real es 204 — discrepancia cosmetica).
            await resp.ShouldBeStatus(HttpStatusCode.NoContent);
            Assert.Empty(await resp.Content.ReadAsStringAsync()); // sin body: ningun tenant filtrado
        }

        [Fact] // TEN-33 — tenants-for-root (root) lista todos los tenants EXCEPTO el propio
        public async Task TenantsForRoot_root_excluye_el_propio()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root (tenant raiz = 1)

            var resp = await client.GetAsync("/api/general/tenants/tenants-for-root");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var lista = await resp.Content.ReadFromJsonAsync<List<TenantHeaderRow>>();
            var ids = lista!.Select(t => t.Id).ToList();
            Assert.Contains(TestData.ActiveTenantId, ids);    // tenant 2
            Assert.Contains(TestData.InactiveTenantId, ids);  // tenant 3
            Assert.DoesNotContain(TestData.RootTenantId, ids); // NO el propio (1)
        }

        [Fact] // TEN-72 — Tenant no es multi-tenant: root accede a cualquier tenant sin simular
        public async Task Root_ve_cualquier_tenant()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root, contexto propio

            var dosT = await client.GetAsync($"/api/general/tenants/{TestData.ActiveTenantId}");
            var tresT = await client.GetAsync($"/api/general/tenants/{TestData.InactiveTenantId}");

            await dosT.ShouldBeStatus(HttpStatusCode.OK);
            await tresT.ShouldBeStatus(HttpStatusCode.OK);
            // Ve el detalle de un tenant ajeno (tenant 3, inactivo) sin filtro multi-tenant.
            Assert.Contains("tenant-c", await tresT.Content.ReadAsStringAsync());
        }
    }
}
