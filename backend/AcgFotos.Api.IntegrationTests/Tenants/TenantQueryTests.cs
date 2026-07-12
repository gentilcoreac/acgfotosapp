using System.Collections.Generic;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — lectura: listado (header liviano), búsqueda, detalle y public-style anónimo. El ABM
    /// y el aislamiento ya están en TenantCrudTests/TenantIsolationTests. Actor root salvo public-style
    /// (AllowAnonymous). Seed: tenants 1 (root), 2 (tenant-b, activo), 3 (tenant-c, inactivo).
    /// </summary>
    public class TenantQueryTests : IntegrationTestBase
    {
        public TenantQueryTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record PublicStyle(string Codigo, string HostName);

        [Fact] // TEN-01 — listado paginado happy: header (Codigo/Nombre/Activo), sin colecciones hijas
        public async Task Listado_paginado_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/tenants");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<TenantHeaderDto>>();
            Assert.True(page!.TotalCount >= 3);
            Assert.Contains(page.Items, t => t.Codigo == "tenant-b");
        }

        [Fact] // TEN-03 — búsqueda por Codigo/Nombre filtra server-side
        public async Task SearchText_filtra()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/tenants?searchText=tenant-b");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<TenantHeaderDto>>();
            Assert.Contains(page!.Items, t => t.Codigo == "tenant-b");
            Assert.DoesNotContain(page.Items, t => t.Codigo == "root");
        }

        [Fact] // TEN-07 — detalle por id happy (TenantDto del ABM)
        public async Task Detalle_por_id_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync($"/api/general/tenants/{TestData.ActiveTenantId}");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("tenant-b", raw);
        }

        [Fact] // TEN-42 — public-style ANÓNIMO por HostName, case-insensitive (GetByHostNameOrCodigo en upper)
        public async Task PublicStyle_por_hostname_case_insensitive()
        {
            await Factory.ExecuteSqlAsync($"UPDATE gen_Tenants SET HostName = 'app.acme.com' WHERE Id = {TestData.ActiveTenantId}");

            using var client = CreateClient(); // sin token
            var resp = await client.GetAsync("/api/general/tenants/public-style/APP.ACME.COM");
            await resp.ShouldBeOk();

            var style = await resp.Content.ReadFromJsonAsync<PublicStyle>();
            Assert.Equal("tenant-b", style!.Codigo); // resolvió el tenant por HostName en mayúsculas
        }

        [Fact] // TEN-44 — public-style con valor inexistente: 200 con body null (no 404)
        public async Task PublicStyle_inexistente_null()
        {
            using var client = CreateClient(); // sin token
            var resp = await client.GetAsync("/api/general/tenants/public-style/noexiste");
            await resp.ShouldBeOk();

            var raw = (await resp.Content.ReadAsStringAsync()).Trim();
            Assert.True(raw == "null" || raw.Length == 0); // sin tenant => null, no error
        }
    }
}
