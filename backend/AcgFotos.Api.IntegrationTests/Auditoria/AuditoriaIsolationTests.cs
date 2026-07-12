using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auditoria
{
    /// <summary>
    /// Auditoria (260) — BASELINE de aislamiento de LECTURA. CAVEAT DE SQL CRUDO: `gen_AuditLogs` (entidad
    /// Auditoria) NO es IMultiTenantEntityBase y NO tiene TenantId -> el filtro global de EF NO la cubre.
    /// El aislamiento es HAND-ROLLED en `AuditoriaRepository`: para un no-root agrega
    /// `Where(x => x.Usuario.TenantId == contexto)` (resuelve el tenant por el usuario auditado); las filas
    /// anonimas (UsuarioId NULL, p.ej. logins fallidos) quedan solo-root. Por eso necesita su PROPIO test
    /// (un refactor que saque ese Where no lo cazaria el filtro global). Las filas se siembran por SQL
    /// (no hace falta drenar la cola async para probar la lectura).
    /// </summary>
    public class AuditoriaIsolationTests : IntegrationTestBase
    {
        public AuditoriaIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        // Siembra 3 filas: tenant 2 (userb=10), tenant 3 (adminb=11) y anonima (UsuarioId NULL).
        private Task SeedAuditRowsAsync() => Factory.ExecuteSqlAsync(@"
            INSERT INTO gen_AuditLogs (FechaHora, Duracion, Servicio, Metodo, UsuarioId, HttpMethod, RequestAbsolutePath, ClientIP, ClientUserAgent, ResultStatusCode, ResultContent) VALUES
                ('2026-06-20T10:00:00', 0, 'SeedT2',   'Get', 10,   'GET', '/x', '127.0.0.1', 'mozilla', '200', 'row-t2'),
                ('2026-06-20T10:01:00', 0, 'SeedT3',   'Get', 11,   'GET', '/x', '127.0.0.1', 'mozilla', '200', 'row-t3'),
                ('2026-06-20T10:02:00', 0, 'SeedAnon', 'Get', NULL, 'GET', '/x', '127.0.0.1', 'mozilla', '401', 'row-anon');");

        private Task<long> IdByContentAsync(string content) =>
            Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_AuditLogs WHERE ResultContent = '{content}'");

        [Fact] // AUD-05/06 — no-root: la grilla solo trae filas de SU tenant; las anonimas quedan ocultas
        public async Task Grilla_no_root_solo_su_tenant()
        {
            await SeedAuditRowsAsync();
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root, tenant 2

            var resp = await client.GetAsync("/api/general/auditoria?page=0&pageSize=200");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            // El listado proyecta liviano (sin ResultContent) -> se afirma por Servicio (sí va en el header).
            Assert.Contains("SeedT2", body);        // su tenant
            Assert.DoesNotContain("SeedT3", body);  // otro tenant -> oculto
            Assert.DoesNotContain("SeedAnon", body); // anonima -> solo-root
        }

        [Fact] // AUD-07 — root ve filas de todos los tenants y las anonimas (sin filtro)
        public async Task Grilla_root_ve_todo()
        {
            await SeedAuditRowsAsync();
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/auditoria?page=0&pageSize=200");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("SeedT2", body);
            Assert.Contains("SeedT3", body);
            Assert.Contains("SeedAnon", body);
        }

        [Fact] // AUD-29 — detalle por id de otro tenant: no-root -> 404 (IDOR bloqueado)
        public async Task Detalle_de_otro_tenant_da_404()
        {
            await SeedAuditRowsAsync();
            var idT3 = await IdByContentAsync("row-t3");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.GetAsync($"/api/general/auditoria/{idT3}");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
            Assert.DoesNotContain("row-t3", await resp.Content.ReadAsStringAsync());
        }

        [Fact] // AUD-30 — detalle de fila anonima: no-root -> 404
        public async Task Detalle_anonimo_da_404_a_no_root()
        {
            await SeedAuditRowsAsync();
            var idAnon = await IdByContentAsync("row-anon");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var resp = await client.GetAsync($"/api/general/auditoria/{idAnon}");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // AUD-37 — el export CSV respeta el mismo aislamiento (no fuga masiva cross-tenant)
        public async Task Csv_no_root_solo_su_tenant()
        {
            await SeedAuditRowsAsync();
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.GetAsync("/api/general/auditoria/csv?pageSize=200");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("SeedT2", body);
            Assert.DoesNotContain("SeedT3", body);
            Assert.DoesNotContain("SeedAnon", body);
        }

        [Fact] // AUD-08 — el filtro de rango de fechas (FechaDesde/FechaHasta) acota el listado
        public async Task Filtro_por_fecha_acota_el_listado()
        {
            await SeedAuditRowsAsync(); // filas del 2026-06-20
            using var client = await CreateAuthenticatedClientAsync(); // root

            // fechaHasta anterior a las filas -> ninguna; rango que las incluye -> aparecen.
            var vacio = await client.GetAsync("/api/general/auditoria?page=0&pageSize=200&fechaHasta=2026-06-19T23:59:59");
            var conRango = await client.GetAsync("/api/general/auditoria?page=0&pageSize=200&fechaDesde=2026-06-20T00:00:00&fechaHasta=2026-06-20T23:59:59");

            await vacio.ShouldBeStatus(HttpStatusCode.OK);
            await conRango.ShouldBeStatus(HttpStatusCode.OK);
            Assert.DoesNotContain("SeedT2", await vacio.Content.ReadAsStringAsync());
            Assert.Contains("SeedT2", await conRango.Content.ReadAsStringAsync());
        }

        [Fact] // AUD — el listado NO trae los campos pesados; el detalle por id SÍ
        public async Task Listado_liviano_detalle_completo()
        {
            await SeedAuditRowsAsync();
            using var client = await CreateAuthenticatedClientAsync(); // root
            var idT2 = await IdByContentAsync("row-t2");

            var lista = await (await client.GetAsync("/api/general/auditoria?page=0&pageSize=200")).Content.ReadAsStringAsync();
            var detalle = await (await client.GetAsync($"/api/general/auditoria/{idT2}")).Content.ReadAsStringAsync();

            // El listado proyecta liviano: el ResultContent ('row-t2') no viaja por fila.
            Assert.DoesNotContain("row-t2", lista);
            // El detalle por id trae el registro completo, incluyendo ResultContent.
            Assert.Contains("row-t2", detalle);
        }
    }
}
