using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — regeneración de themes CSS (I/O de archivos vía IStorageProvider). Dos endpoints,
    /// ambos gateados por EnsureRootCaller (`IsRoot && TenantId==RootTenantId`) — antes eran [AllowAnonymous]
    /// (cualquiera forzaba I/O = DoS). El host de tests tiene storage + template-base.css funcional (lo
    /// confirma el alta de TenantCrudTests, que regenera). Tenant 2 arranca SIN StyleSheetCssUrl.
    /// </summary>
    public class TenantThemeTests : IntegrationTestBase
    {
        public TenantThemeTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // TEN-47 — regenerar-theme: root del tenant raíz regenera el CSS base (200, sin custom theme)
        public async Task Regenerar_theme_root_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root, tenant raíz

            var resp = await client.GetAsync($"/api/general/tenants/regenerar-theme-por-tenant-id/{TestData.ActiveTenantId}");

            await resp.ShouldBeOk();
        }

        [Fact] // TEN-48 — regenerar-theme: un no-root NO puede (EnsureRootCaller → 400), sin I/O
        public async Task Regenerar_theme_no_root_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.GetAsync($"/api/general/tenants/regenerar-theme-por-tenant-id/{TestData.ActiveTenantId}");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }

        [Fact] // TEN-50 — regenerar-theme: tenant inexistente → 400 ErrorTenantNotFound (tras pasar el gate)
        public async Task Regenerar_theme_tenant_inexistente()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/tenants/regenerar-theme-por-tenant-id/999999");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantNotFound);
        }

        [Fact] // TEN-51 — regenerar-theme: tenant con custom theme (StyleSheetCssUrl) → 400, NO pisa el CSS subido
        public async Task Regenerar_theme_custom_theme_rechazado()
        {
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Tenants SET StyleSheetCssUrl = 'http://x/custom.css' WHERE Id = {TestData.ActiveTenantId}");

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.GetAsync($"/api/general/tenants/regenerar-theme-por-tenant-id/{TestData.ActiveTenantId}");

            await resp.ShouldBeBadRequest("El tenant utiliza un custom theme.");
        }

        [Fact] // TEN-52 — regenerar-todos: root raíz regenera solo los tenants sin custom theme (200)
        public async Task Regenerar_todos_root_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/tenants/regenerar-todos-los-themes");

            await resp.ShouldBeOk();
        }

        [Fact] // TEN-53 — regenerar-todos: un no-root NO puede (EnsureRootCaller corta antes de iterar)
        public async Task Regenerar_todos_no_root_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.GetAsync("/api/general/tenants/regenerar-todos-los-themes");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }
    }
}
