using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — administradores del tenant (lectura cross-tenant CORRECTA vía IgnoreQueryFilters,
    /// contraste con #18) y sync de las colecciones del ABM (TenantAplicaciones/TenantLicenses por clave
    /// de negocio). Actor = root. El edit dispara el doble-save+theme (funciona en el host de tests).
    /// </summary>
    public class TenantAdminSyncTests : IntegrationTestBase
    {
        public TenantAdminSyncTests(TestWebApplicationFactory factory) : base(factory) { }

        // Edit del tenant 2 variando solo las colecciones (codigo/nombre existentes para el check de edición).
        private static object EditTenant2(object[] apps, object[] licenses) => new
        {
            id = TestData.ActiveTenantId,
            codigo = "tenant-b",
            nombre = "Tenant B",
            tituloWeb = "Tenant B",
            activo = true,
            tenantAplicaciones = apps,
            tenantLicenses = licenses,
        };

        // Licencia tipo1 "tal cual" (preserva el cupo existente del tenant 2 en los edits que tocan apps).
        private static object Tipo1Keep => new { tipoLicenciaId = 1L, cantidad = 5L, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" };

        // --- Administradores (TEN-37/38) ---

        [Fact] // TEN-37 — root ve los admins de OTRO tenant (IgnoreQueryFilters; el filtro global los ocultaría)
        public async Task Administradores_cross_tenant_via_ignore_filters()
        {
            // "Admin" = usuario con un rol EsDefaultParaNuevoTenant. Sembramos eso para adminb2 (tenant 2).
            await Factory.ExecuteSqlAsync("UPDATE gen_Roles SET EsDefaultParaNuevoTenant = 1 WHERE Id = 1");
            await Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES ({TestData.AdminB2Id}, 1, {TestData.ActiveTenantId})"); // adminb2 -> Rol 1

            using var client = await CreateAuthenticatedClientAsync(); // root (contexto = tenant raíz)
            var resp = await client.GetAsync($"/api/general/tenants/{TestData.ActiveTenantId}/administradores");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("adminb2", body); // root ve el admin del tenant 2 pese a ser otra tenancy
        }

        [Fact] // TEN-38 — tenant sin admins -> lista vacía (no 404)
        public async Task Administradores_tenant_sin_admins_vacio()
        {
            await Factory.ExecuteSqlAsync("UPDATE gen_Roles SET EsDefaultParaNuevoTenant = 1 WHERE Id = 1");
            // Nadie del tenant 3 tiene el rol default.

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.GetAsync($"/api/general/tenants/{TestData.InactiveTenantId}/administradores");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var admins = await resp.Content.ReadFromJsonAsync<List<object>>();
            Assert.Empty(admins!);
        }

        // --- Sync de colecciones (TEN-19/20/22) ---

        [Fact] // TEN-19 — agrega una aplicación al tenant (SyncBy por AplicacionId)
        public async Task Sync_agrega_aplicacion()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update",
                EditTenant2(apps: new object[] { new { aplicacionId = 1L } }, licenses: new[] { Tipo1Keep }));

            await resp.ShouldBeOk();
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TenantAplicaciones WHERE TenantId = {TestData.ActiveTenantId} AND AplicacionId = 1"));
        }

        [Fact] // TEN-20 — quita una aplicación del tenant (SyncBy elimina la fila que ya no viene)
        public async Task Sync_quita_aplicacion()
        {
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_TenantAplicaciones (TenantId, AplicacionId) VALUES ({TestData.ActiveTenantId}, 1)");

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update",
                EditTenant2(apps: System.Array.Empty<object>(), licenses: new[] { Tipo1Keep }));

            await resp.ShouldBeOk();
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_TenantAplicaciones WHERE TenantId = {TestData.ActiveTenantId} AND AplicacionId = 1"));
        }

        [Fact] // TEN-22 — agrega un tipo de licencia (SyncBy por TipoLicenciaId; conserva el existente)
        public async Task Sync_agrega_tipo_licencia()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var nueva = new { tipoLicenciaId = 2L, cantidad = 3L, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" };
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update",
                EditTenant2(apps: System.Array.Empty<object>(), licenses: new object[] { Tipo1Keep, nueva }));

            await resp.ShouldBeOk();
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TenantLicencias WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1")); // conservada
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TenantLicencias WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 2")); // agregada
        }
    }
}
