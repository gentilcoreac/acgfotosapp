using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — cierre del ABM por root: edición de escalares de licencias existentes, idempotencia
    /// de colecciones (SyncBy sin churn) y baja (happy + bloqueo por FK). La edición happy de escalares del
    /// tenant ya está en TenantLicenseQuotaTests (LIC-45, TituloWeb). Actor = root. (TEN-18 desactivar→security
    /// stamp se difiere: el target de UpdateSecurityStampToTenantUsersAsync es el tenant del contexto, no el
    /// editado — riesgo latente ya anotado, requiere su propio análisis.)
    /// </summary>
    public class TenantEditTests : IntegrationTestBase
    {
        public TenantEditTests(TestWebApplicationFactory factory) : base(factory) { }

        // Edición del tenant 2 con un set de apps y de licencias.
        private static object EditarTenant2(long[] aplicacionIds, long cantidadTipo1 = 5) => new
        {
            id = TestData.ActiveTenantId,
            codigo = "tenant-b",
            nombre = "Tenant B",
            tituloWeb = "Tenant B",
            activo = true,
            tenantAplicaciones = System.Array.ConvertAll(aplicacionIds, a => (object)new { aplicacionId = a }),
            tenantLicenses = new[]
            {
                new { tipoLicenciaId = 1L, cantidad = cantidadTipo1, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" }
            },
        };

        [Fact] // TEN-23 — edición actualiza los escalares de la licencia existente (Cantidad) sin recrear la fila
        public async Task Edicion_actualiza_cantidad_de_licencia_existente()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", EditarTenant2(new long[] { 1 }, cantidadTipo1: 10));
            await resp.ShouldBeOk();

            Assert.Equal(10, await Factory.QueryScalarAsync<long>(
                $"SELECT Cantidad FROM gen_TenantLicencias WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1"));
            // Sigue siendo UNA sola fila (se actualizó, no se recreó).
            Assert.Equal(1, await CountAsync(
                $"SELECT COUNT(*) FROM gen_TenantLicencias WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1"));
        }

        [Fact] // TEN-21/71 — idempotencia: dos updates con el mismo set de apps no churnean la colección (misma fila)
        public async Task Update_repetido_no_churnea_colecciones()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            await (await client.PostAsJsonAsync("/api/general/tenants/update", EditarTenant2(new long[] { 1 }))).ShouldBeOk();
            long idApp1 = await Factory.QueryScalarAsync<long>(
                $"SELECT Id FROM gen_TenantAplicaciones WHERE TenantId = {TestData.ActiveTenantId} AND AplicacionId = 1");

            await (await client.PostAsJsonAsync("/api/general/tenants/update", EditarTenant2(new long[] { 1 }))).ShouldBeOk();

            // Misma fila (no DELETE+INSERT): el Id de la asociación se conserva y no hay duplicados.
            Assert.Equal(1, await CountAsync(
                $"SELECT COUNT(*) FROM gen_TenantAplicaciones WHERE TenantId = {TestData.ActiveTenantId} AND AplicacionId = 1"));
            Assert.Equal(idApp1, await Factory.QueryScalarAsync<long>(
                $"SELECT Id FROM gen_TenantAplicaciones WHERE TenantId = {TestData.ActiveTenantId} AND AplicacionId = 1"));
        }

        [Fact] // TEN-26 — delete happy: un tenant sin dependencias se borra
        public async Task Delete_happy()
        {
            // Tenant "pelado" sembrado por SQL (sin usuarios/licencias/apps que lo referencien).
            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Tenants (Codigo, Nombre, Activo, DarkModeByDefault, TipoLayoutLogin, " +
                "LogoLoginLightUrl, LogoLoginDarkUrl, LogoHeaderLightUrl, LogoHeaderDarkUrl, FaviconUrl, ErrorDescription, HasError) " +
                "VALUES ('del-bare', 'Del Bare', true, false, 0, '', '', '', '', '', '', false)");
            long id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Tenants WHERE Codigo = 'del-bare'");

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.DeleteAsync($"/api/general/tenants/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_Tenants WHERE Id = {id}"));
        }

        [Fact] // TEN-27 — FIX #19: con la FK gen_Usuarios.TenantId → gen_Tenants (OnDelete Restrict), borrar un
               // tenant que TODAVÍA tiene usuarios queda BLOQUEADO (error 547 → 400 vía el middleware), en vez de
               // dejar usuarios huérfanos. La baja real de un tenant es la DESACTIVACIÓN (Activo=false), no el
               // hard-delete. El tenant y sus usuarios quedan intactos.
        public async Task Delete_tenant_con_usuarios_bloqueado_por_fk()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            Assert.True(await CountAsync($"SELECT COUNT(*) FROM gen_Usuarios WHERE TenantId = {TestData.ActiveTenantId}") > 0); // precond

            var resp = await client.DeleteAsync($"/api/general/tenants/{TestData.ActiveTenantId}");

            Assert.False(resp.IsSuccessStatusCode);
            Assert.NotEqual(System.Net.HttpStatusCode.InternalServerError, resp.StatusCode); // 400 controlado, no 500 crudo
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}"));          // tenant intacto
            Assert.True(await CountAsync($"SELECT COUNT(*) FROM gen_Usuarios WHERE TenantId = {TestData.ActiveTenantId}") > 0);    // usuarios intactos (sin orfanar)
        }
    }
}
