using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Aplicaciones (180) — las tres lecturas auxiliares y sus reglas de contexto/aislamiento:
    ///  - aplicaciones-permitidas: apps del usuario del contexto (AppContext.UserId).
    ///  - aplicaciones-tenant: apps del tenant del contexto (AppContext.TenantId) — aislamiento entre tenants.
    ///  - aplicaciones-tenant-id/{id}: consulta administrativa cross-tenant, GATEADA a IsRoot && TenantId==RootTenantId.
    /// Arrange (inline): gen_UsuarioAplicaciones para userb→app1; gen_TenantAplicaciones para tenant 2→app1.
    /// </summary>
    public class AplicacionAuxReadsTests : IntegrationTestBase
    {
        public AplicacionAuxReadsTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record Permitida(long AplicacionId);

        // gen_UsuarioAplicaciones es multi-tenant (TenantId NOT NULL) → se sella con el tenant del usuario.
        private Task SeedUsuarioAppAsync(long usuarioId, long aplicacionId, long tenantId = TestData.ActiveTenantId) =>
            Factory.ExecuteSqlAsync($"INSERT INTO gen_UsuarioAplicaciones (UsuarioId, AplicacionId, TenantId, \"default\") VALUES ({usuarioId}, {aplicacionId}, {tenantId}, false)");

        private Task SeedTenantAppAsync(long tenantId, long aplicacionId) =>
            Factory.ExecuteSqlAsync($"INSERT INTO gen_TenantAplicaciones (TenantId, AplicacionId) VALUES ({tenantId}, {aplicacionId})");

        private async Task<HttpClient> ImpersonatedClientAsync(long tenantId, long userId)
        {
            var token = await ApiClient.ImpersonationTokenAsync(Factory, tenantId, userId);
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // ---- aplicaciones-permitidas (apps del usuario del contexto) ----

        [Fact] // APP-34/35 — devuelve las apps del usuario del contexto (AppContext.UserId), no las de otro
        public async Task Permitidas_del_usuario_del_contexto()
        {
            await SeedUsuarioAppAsync(TestData.UserBId, 1); // solo userb tiene la app

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var resp = await client.GetAsync("/api/general/aplicaciones/aplicaciones-permitidas");
            await resp.ShouldBeOk();

            var apps = await resp.Content.ReadFromJsonAsync<List<Permitida>>();
            Assert.Contains(apps!, a => a.AplicacionId == 1);
        }

        [Fact] // APP-36 — usuario sin apps: lista vacía (no null/500)
        public async Task Permitidas_usuario_sin_apps_vacio()
        {
            await SeedUsuarioAppAsync(TestData.UserBId, 1); // userb sí; adminb2 no

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.GetAsync("/api/general/aplicaciones/aplicaciones-permitidas");
            await resp.ShouldBeOk();

            var apps = await resp.Content.ReadFromJsonAsync<List<Permitida>>();
            Assert.Empty(apps!);
        }

        [Fact] // APP-37 — bajo impersonación usa el usuario DESTINO (no root): apps de userb
        public async Task Permitidas_impersonando_usa_usuario_destino()
        {
            await SeedUsuarioAppAsync(TestData.UserBId, 1);

            using var client = await ImpersonatedClientAsync(TestData.ActiveTenantId, TestData.UserBId);
            var resp = await client.GetAsync("/api/general/aplicaciones/aplicaciones-permitidas");
            await resp.ShouldBeOk();

            var apps = await resp.Content.ReadFromJsonAsync<List<Permitida>>();
            Assert.Contains(apps!, a => a.AplicacionId == 1); // del usuario simulado, no de root
        }

        // ---- aplicaciones-tenant (apps del tenant del contexto) ----

        [Fact] // APP-42/43 — apps del tenant del contexto; aislamiento: tenant 3 (sin apps) no ve las de tenant 2
        public async Task Tenant_apps_del_contexto_con_aislamiento()
        {
            await SeedTenantAppAsync(TestData.ActiveTenantId, 1); // solo tenant 2 tiene la app

            using var clientB = await CreateAuthenticatedClientAsync(TestData.UserB);   // tenant 2
            var apps2 = await (await clientB.GetAsync("/api/general/aplicaciones/aplicaciones-tenant"))
                .Content.ReadFromJsonAsync<List<AplicacionDto>>();
            Assert.Contains(apps2!, a => a.Id == 1);

            using var clientC = await CreateAuthenticatedClientAsync(TestData.AdminB);   // tenant 3, sin TenantAplicacion
            var apps3 = await (await clientC.GetAsync("/api/general/aplicaciones/aplicaciones-tenant"))
                .Content.ReadFromJsonAsync<List<AplicacionDto>>();
            Assert.DoesNotContain(apps3!, a => a.Id == 1);
        }

        [Fact] // APP-45 — bajo impersonación usa el TenantId del DESTINO (tenant 2), no el raíz
        public async Task Tenant_apps_impersonando_usa_tenant_destino()
        {
            await SeedTenantAppAsync(TestData.ActiveTenantId, 1);

            using var client = await ImpersonatedClientAsync(TestData.ActiveTenantId, TestData.UserBId);
            var apps = await (await client.GetAsync("/api/general/aplicaciones/aplicaciones-tenant"))
                .Content.ReadFromJsonAsync<List<AplicacionDto>>();

            Assert.Contains(apps!, a => a.Id == 1); // del tenant 2 (simulado); si usara el raíz sería vacío
        }

        // ---- aplicaciones-tenant-id/{id} (administrativo, gateado a root del tenant raíz) ----

        [Fact] // APP-48 — root del tenant raíz consulta las apps de un tenant arbitrario por id
        public async Task TenantId_root_consulta_cross_tenant()
        {
            await SeedTenantAppAsync(TestData.ActiveTenantId, 1);

            using var client = await CreateAuthenticatedClientAsync(); // root, tenant raíz
            var resp = await client.GetAsync($"/api/general/aplicaciones/aplicaciones-tenant-id/{TestData.ActiveTenantId}");
            await resp.ShouldBeOk();

            var apps = await resp.Content.ReadFromJsonAsync<List<AplicacionDto>>();
            Assert.Contains(apps!, a => a.Id == 1);
        }

        [Fact] // APP-49 — no-root: rechazo server-side (ErrorUserNotPrivilegesAccess), no devuelve apps
        public async Task TenantId_no_root_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root
            var resp = await client.GetAsync($"/api/general/aplicaciones/aplicaciones-tenant-id/{TestData.ActiveTenantId}");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }

        [Fact] // APP-50 — root impersonando (TenantId != raíz): la guarda exige IsRoot AND tenant raíz → rechazo
        public async Task TenantId_root_impersonando_rechazado()
        {
            using var client = await ImpersonatedClientAsync(TestData.ActiveTenantId, TestData.UserBId);
            var resp = await client.GetAsync($"/api/general/aplicaciones/aplicaciones-tenant-id/{TestData.ActiveTenantId}");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }

        [Fact] // APP-52/53 — root tenant raíz, tenant target sin apps (o inexistente): lista vacía, no 500
        public async Task TenantId_target_sin_apps_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.GetAsync("/api/general/aplicaciones/aplicaciones-tenant-id/999999");
            await resp.ShouldBeOk();

            var apps = await resp.Content.ReadFromJsonAsync<List<AplicacionDto>>();
            Assert.Empty(apps!);
        }
    }
}
