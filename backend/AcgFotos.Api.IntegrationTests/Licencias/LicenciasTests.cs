using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Licencias
{
    /// <summary>
    /// Licencias (280) — endpoints tenant-scopeados del UsuarioController: resumen de disponibilidad/
    /// vencimiento por tipo (`get-cantidad-licencias-asignadas`) y usuarios con licencia activa
    /// (`usuarios-con-licencia`). **Lidera con aislamiento** (LIC-08/15): el conteo y el listado SOLO
    /// cuentan el tenant del contexto. Caveat MT: `UsuarioTipoLicencia` ES IMultiTenantEntityBase, pero
    /// el conteo del resumen filtra explicito por `Usuario.TenantId` (no por el filtro global) — por eso
    /// el aislamiento se prueba aparte. Seed tenant 2: userb(10)/userb2(14)/adminb2(15) con tipo1 activa;
    /// tenant 3: adminb(11) tipo1 activa. TenantLicencia: tenant2 tipo1 Cantidad5 (vigente 2099).
    /// </summary>
    public class LicenciasTests : IntegrationTestBase
    {
        public LicenciasTests(TestWebApplicationFactory factory) : base(factory) { }

        private const long Tipo1 = 1; // Visualizador
        private const long Tipo2 = 2; // Admin del Tenant Cliente

        private sealed record Cupo(
            long TipoLicenciaId, int CantidadTotal, int CantidadAsignada, int CantidadDisponible,
            bool IsExpired, bool IsExpiringSoon);
        private sealed record UsuarioRow(string UserName);

        private static Task<List<Cupo>> GetResumenAsync(HttpClient client) =>
            client.GetFromJsonAsync<List<Cupo>>("/api/general/usuarios/get-cantidad-licencias-asignadas")!;

        private static Task<List<UsuarioRow>> GetConLicenciaAsync(HttpClient client) =>
            client.GetFromJsonAsync<List<UsuarioRow>>("/api/general/usuarios/usuarios-con-licencia")!;

        private Task SeedUserLicenseAsync(long usuarioId, long tipoLicenciaId, long tenantId, bool activa) =>
            Factory.ExecuteSqlAsync(
                $@"INSERT INTO gen_UsuarioTipoLicencia (UsuarioId, TipoLicenciaId, CreatedDatetime, TenantId, IsActive)
                   VALUES ({usuarioId}, {tipoLicenciaId}, now(), {tenantId}, {(activa ? "true" : "false")})");

        // ---------------- get-cantidad-licencias-asignadas ----------------

        [Fact] // LIC-01 — resumen happy: total/asignada/disponible por tipo del tenant del contexto
        public async Task Resumen_calcula_disponibilidad_por_tipo()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // tenant 2

            var tipo1 = Assert.Single(await GetResumenAsync(client), c => c.TipoLicenciaId == Tipo1);
            Assert.Equal(5, tipo1.CantidadTotal);      // TenantLicencia cupo
            Assert.Equal(3, tipo1.CantidadAsignada);   // userb + userb2 + adminb2
            Assert.Equal(2, tipo1.CantidadDisponible); // 5 - 3
            Assert.False(tipo1.IsExpired);
        }

        [Fact] // LIC-08 — el conteo aisla por tenant y excluye asignaciones inactivas
        public async Task Resumen_excluye_inactivas_y_otros_tenants()
        {
            // Ruido: una inactiva del propio tenant (no debe contar) + tenant 3 ya tiene una activa (adminb).
            await SeedUserLicenseAsync(TestData.PendingId, Tipo1, TestData.ActiveTenantId, activa: false);

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // tenant 2

            var tipo1 = Assert.Single(await GetResumenAsync(client), c => c.TipoLicenciaId == Tipo1);
            Assert.Equal(3, tipo1.CantidadAsignada); // sigue 3: ni la inactiva ni la del tenant 3 cuentan
        }

        [Fact] // LIC-03 — IsExpired cuando ExpireDatetime <= now
        public async Task Resumen_marca_vencida()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // login con licencia vigente
            // Vence DESPUES del login (el gate de licencia es solo en login/refresh; el GET recomputa en vivo).
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_TenantLicencias SET ExpireDatetime = now() - interval '1 day' WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1");

            var tipo1 = Assert.Single(await GetResumenAsync(client), c => c.TipoLicenciaId == Tipo1);
            Assert.True(tipo1.IsExpired);
            Assert.False(tipo1.IsExpiringSoon); // excluido por !IsExpired
        }

        [Fact] // LIC-04 — IsExpiringSoon dentro del umbral (default 30 dias)
        public async Task Resumen_marca_proximo_a_vencer()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_TenantLicencias SET ExpireDatetime = now() + interval '10 days' WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1");

            var tipo1 = Assert.Single(await GetResumenAsync(client), c => c.TipoLicenciaId == Tipo1);
            Assert.False(tipo1.IsExpired);
            Assert.True(tipo1.IsExpiringSoon); // <= now + 30d
        }

        [Fact] // LIC-07 — tipo licenciado sin asignaciones: Asignada=0, Disponible=Total (sin NRE)
        public async Task Resumen_tipo_sin_asignaciones()
        {
            // Tenant 2 contrata tipo2 (cupo 4) pero nadie lo tiene asignado.
            await Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_TenantLicencias (TenantId, TipoLicenciaId, Cantidad, StartDatetime, ExpireDatetime, ModifiedDatetime)
                VALUES ({TestData.ActiveTenantId}, 2, 4, now(), '2099-12-31', now())");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var tipo2 = Assert.Single(await GetResumenAsync(client), c => c.TipoLicenciaId == Tipo2);
            Assert.Equal(0, tipo2.CantidadAsignada);
            Assert.Equal(4, tipo2.CantidadDisponible);
        }

        [Fact] // LIC-13 — tenant sin TenantLicencia -> lista vacia (no 500)
        public async Task Resumen_tenant_sin_licencias_devuelve_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            await Factory.ExecuteSqlAsync($"DELETE FROM gen_TenantLicencias WHERE TenantId = {TestData.ActiveTenantId}");

            Assert.Empty(await GetResumenAsync(client));
        }

        // ---------------- usuarios-con-licencia ----------------

        [Fact] // LIC-14/15 — lista los usuarios con licencia activa SOLO del tenant del contexto
        public async Task UsuariosConLicencia_aisla_por_tenant()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // tenant 2

            var names = (await GetConLicenciaAsync(client)).Select(u => u.UserName).ToList();

            Assert.Contains("userb", names);
            Assert.Contains("userb2", names);
            Assert.Contains("adminb2", names);
            Assert.DoesNotContain("adminb", names); // tenant 3
            Assert.DoesNotContain("userc", names);  // tenant 3 (ademas sin licencia)
            Assert.DoesNotContain("root", names);   // tenant 1
        }

        [Fact] // LIC-16 — de-duplica (un user con 2 licencias activas) y excluye los solo-inactivos
        public async Task UsuariosConLicencia_dedup_y_excluye_inactivos()
        {
            await SeedUserLicenseAsync(TestData.UserBId, Tipo2, TestData.ActiveTenantId, activa: true); // userb 2da activa
            await SeedUserLicenseAsync(TestData.PendingId, Tipo1, TestData.ActiveTenantId, activa: false); // pending solo inactiva

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var names = (await GetConLicenciaAsync(client)).Select(u => u.UserName).ToList();
            Assert.Single(names, n => n == "userb"); // aparece UNA vez pese a 2 licencias activas
            Assert.DoesNotContain("pending", names); // solo tiene inactiva -> fuera
        }
    }
}
