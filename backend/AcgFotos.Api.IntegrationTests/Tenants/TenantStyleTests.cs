using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — branding/estilo. `public-style/{valueToFilter}` es [AllowAnonymous] (lo usa el
    /// login ANTES de autenticar para resolver logos/colores por Codigo o HostName) → debe exponer SOLO
    /// branding público, nunca datos internos (HasError/ErrorDescription/licencias/usuarios). `header-style/{id}`
    /// es autenticado. Endpoints de solo lectura, deterministas.
    /// </summary>
    public class TenantStyleTests : IntegrationTestBase
    {
        public TenantStyleTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record PublicStyle(string Codigo, string TituloWeb);

        [Fact] // TEN-41 — public-style por Codigo, ANÓNIMO (sin token) -> 200 con el branding del tenant
        public async Task PublicStyle_anonimo_por_codigo()
        {
            using var client = CreateClient(); // sin Authorization (AllowAnonymous)

            var resp = await client.GetAsync("/api/general/tenants/public-style/tenant-b");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var style = await resp.Content.ReadFromJsonAsync<PublicStyle>();
            Assert.Equal("tenant-b", style!.Codigo);
        }

        [Fact] // TEN-45 — public-style NO filtra datos internos del tenant (anónimo)
        public async Task PublicStyle_no_filtra_datos_sensibles()
        {
            // El sistema marcó el tenant 2 con un error interno (no debe verse desde el endpoint público).
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Tenants SET HasError = true, ErrorDescription = 'secreto-error-t2' WHERE Id = {TestData.ActiveTenantId}");

            using var client = CreateClient(); // anónimo
            var resp = await client.GetAsync("/api/general/tenants/public-style/tenant-b");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.DoesNotContain("secreto-error-t2", body); // ErrorDescription no se expone
            Assert.DoesNotContain("errorDescription", body);  // ni el campo
        }

        [Fact] // TEN-43 — public-style con valor inexistente -> success sin datos (no 404, no error)
        public async Task PublicStyle_valor_inexistente_no_devuelve_tenant()
        {
            using var client = CreateClient(); // anónimo

            var resp = await client.GetAsync("/api/general/tenants/public-style/noexiste");

            await resp.ShouldBeOk();
            Assert.DoesNotContain("tenant-b", await resp.Content.ReadAsStringAsync()); // no resolvió ningún tenant
        }

        [Fact] // TEN-29 — header-style por Id, autenticado -> 200
        public async Task HeaderStyle_por_id_autenticado()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync($"/api/general/tenants/header-style/{TestData.ActiveTenantId}");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // TEN-30 — header-style de un Id inexistente -> 404
        public async Task HeaderStyle_id_inexistente_da_404()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/tenants/header-style/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
