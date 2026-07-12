using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// actividad-mensual (USR-89/90/93): serie del homepage (Activos por logins + Altas por DateCreated),
    /// ventana de N meses con relleno de ceros. El aislamiento multi-tenant es por el filtro global de EF:
    /// Activos sale de `UsuarioHistorial` (IMultiTenantEntityBase) y Altas de `Usuario` (idem) -> un tenant
    /// solo cuenta SU actividad. Se siembran logins por SQL en gen_UsuariosHistorial.
    /// </summary>
    public class UsuarioActividadTests : IntegrationTestBase
    {
        public UsuarioActividadTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record ActividadEntry(int Anio, int Mes, int Activos, int Altas);

        [Fact] // USR-89 — happy path: la ventana default trae 12 entradas
        public async Task Serie_default_trae_12_meses()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/usuarios/actividad-mensual");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var serie = await resp.Content.ReadFromJsonAsync<List<ActividadEntry>>();
            Assert.Equal(12, serie!.Count); // relleno explicito: siempre `meses` entradas, sin huecos
        }

        [Fact] // USR-90 — ?meses=6 trae exactamente 6 entradas (relleno con ceros, sin gaps)
        public async Task Serie_respeta_la_ventana_pedida()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/usuarios/actividad-mensual?meses=6");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var serie = await resp.Content.ReadFromJsonAsync<List<ActividadEntry>>();
            Assert.Equal(6, serie!.Count);
        }

        [Fact] // USR-93 — AISLAMIENTO: cada tenant cuenta solo SU actividad (filtro global sobre UsuarioHistorial)
        public async Task Actividad_aisla_por_tenant()
        {
            // Logins del mes actual: 1 usuario distinto en tenant 2 (10) y 2 en tenant 3 (11, 12).
            await Factory.ExecuteSqlAsync(@"
                INSERT INTO gen_UsuariosHistorial (UsuarioId, Periodo, FechaLastLogin, TenantId) VALUES
                    (10, 'p', SYSDATETIME(), 2),
                    (11, 'p', SYSDATETIME(), 3),
                    (12, 'p', SYSDATETIME(), 3);");

            using (var t2 = await CreateAuthenticatedClientAsync(TestData.UserB))   // tenant 2
            {
                var serie = await (await t2.GetAsync("/api/general/usuarios/actividad-mensual"))
                    .Content.ReadFromJsonAsync<List<ActividadEntry>>();
                Assert.Equal(1, serie![^1].Activos); // mes actual: solo el login del tenant 2 (si filtrara mal, 3)
            }

            using (var t3 = await CreateAuthenticatedClientAsync(TestData.AdminB))  // tenant 3 (admin con licencia: sí loguea)
            {
                var serie = await (await t3.GetAsync("/api/general/usuarios/actividad-mensual"))
                    .Content.ReadFromJsonAsync<List<ActividadEntry>>();
                Assert.Equal(2, serie![^1].Activos); // mes actual: los 2 logins del tenant 3, ninguno del 2
            }
        }

        [Fact] // ACT-05 — Activos cuenta usuarios DISTINTOS por mes: varios logins del mismo user = 1
        public async Task Activos_cuenta_usuarios_distintos()
        {
            // 2 logins del MISMO usuario (10) en el tenant 2, mes actual.
            await Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_UsuariosHistorial (UsuarioId, Periodo, FechaLastLogin, TenantId) VALUES
                    ({TestData.UserBId}, 'p', SYSDATETIME(), {TestData.ActiveTenantId}),
                    ({TestData.UserBId}, 'p', SYSDATETIME(), {TestData.ActiveTenantId});");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // único user del t2 que loguea
            var serie = await (await client.GetAsync("/api/general/usuarios/actividad-mensual"))
                .Content.ReadFromJsonAsync<List<ActividadEntry>>();

            Assert.Equal(1, serie![^1].Activos); // DISTINCT: el user 10 cuenta una vez aunque tenga N filas
        }

        [Fact] // ACT-06 — Altas se cuentan por DateCreated del mes (guarda el bug histórico de DateCreated en 0001)
        public async Task Altas_cuentan_por_datecreated_del_mes()
        {
            // userb2 (t2) "dado de alta" este mes.
            await Factory.ExecuteSqlAsync($"UPDATE gen_Usuarios SET DateCreated = SYSDATETIME() WHERE Id = {TestData.UserB2Id}");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var serie = await (await client.GetAsync("/api/general/usuarios/actividad-mensual"))
                .Content.ReadFromJsonAsync<List<ActividadEntry>>();

            Assert.True(serie![^1].Altas >= 1); // el alta del mes corriente se refleja (no queda en 0)
        }

        [Fact] // ACT-11 — meses=0: comportamiento definido (serie vacía), no excepción 500
        public async Task Meses_cero_no_revienta()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/usuarios/actividad-mensual?meses=0");

            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
        }
    }
}
