using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — edit-by-admin-client: self-service del branding por el admin del PROPIO tenant.
    /// El rechazo cross-tenant (#13: Id del body != tenant del caller) y el no-admin (TEN-56) ya están en
    /// TenantIsolationTests. Acá: happy del propio tenant, que no toca identidad (Codigo/Nombre/HostName),
    /// y el GOTCHA TEN-62 (sin StyleSheetCssUrl el save dispara la regeneración root-only → el admin legítimo
    /// queda bloqueado). Actor = adminb2 (Administrador del tenant 2). Para el happy se setea StyleSheetCssUrl
    /// (evita la regeneración). Self-service ⇒ el Id del input debe ser el del tenant del caller (2).
    /// </summary>
    public class TenantEditByAdminTests : IntegrationTestBase
    {
        public TenantEditByAdminTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task SetCustomThemeAsync() =>
            Factory.ExecuteSqlAsync(
                $"UPDATE gen_Tenants SET StyleSheetCssUrl = 'http://x/custom.css' WHERE Id = {TestData.ActiveTenantId}");

        [Fact] // TEN-55 — happy: el admin del tenant 2 autogestiona su branding (con custom theme, sin regenerar)
        public async Task Admin_edita_branding_propio()
        {
            await SetCustomThemeAsync();

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin del tenant 2
            var resp = await client.PostAsJsonAsync("/api/general/tenants/edit-by-admin-client",
                new { id = TestData.ActiveTenantId, tituloWeb = "Branding Nuevo", activo = true, styleSheetCssUrl = "http://x/custom.css" });
            await resp.ShouldBeOk();

            Assert.Equal("Branding Nuevo",
                await Factory.QueryScalarAsync<string>($"SELECT TituloWeb FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}"));
        }

        [Fact] // TEN-58 — el input NO expone Codigo/Nombre/HostName: campos extra en el body se ignoran
        public async Task Admin_no_cambia_identidad_del_tenant()
        {
            await SetCustomThemeAsync();

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/tenants/edit-by-admin-client",
                new { id = TestData.ActiveTenantId, tituloWeb = "X", activo = true, styleSheetCssUrl = "http://x/custom.css",
                      codigo = "HACK", nombre = "HACK", hostName = "hack.com" });
            await resp.ShouldBeOk();

            // Codigo/Nombre/HostName intactos (no están en TenantEditByAdminClientInput → ApplyAdminClientInputToDto no los toca).
            Assert.Equal("tenant-b", await Factory.QueryScalarAsync<string>($"SELECT Codigo FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}"));
            Assert.Equal("Tenant B", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}"));
        }

        [Fact] // TEN-62 — GOTCHA: sin StyleSheetCssUrl el save dispara RegenerarTheme → EnsureRootCaller → el
               // admin del cliente (no root) queda bloqueado (400 ErrorUserNotPrivilegesAccess). Acoplamiento root-only.
        public async Task Admin_sin_css_bloqueado_por_regeneracion()
        {
            // tenant 2 arranca SIN StyleSheetCssUrl (seed) → el path de save regenera.
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/tenants/edit-by-admin-client",
                new { id = TestData.ActiveTenantId, tituloWeb = "X", activo = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }
    }
}
