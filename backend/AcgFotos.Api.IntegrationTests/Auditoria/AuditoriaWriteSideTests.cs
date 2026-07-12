using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auditoria
{
    /// <summary>
    /// Write-side del audit log (AUD-56): el acceso a un endpoint se auto-audita. El AuditLogFilter
    /// (OnResultExecuted) ENCOLA el evento; en prod lo persiste async el AuditLogBackgroundWriter (ADR-0005).
    /// En tests el writer esta apagado (raceaba con Respawn): tras la request el evento ya esta encolado,
    /// asi que se drena la cola a mano (DrainAuditQueue) de forma DETERMINISTICA y se verifica en la base.
    /// Marcador: User-Agent propio de la request.
    /// </summary>
    public class AuditoriaWriteSideTests : IntegrationTestBase
    {
        public AuditoriaWriteSideTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUD-56 — el acceso al log se auto-audita (Servicio=Auditoria, 200)
        public async Task Acceso_al_log_se_auto_audita()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "aud56-probe");

            var resp = await client.GetAsync("/api/general/auditoria");
            await resp.ShouldBeStatus(HttpStatusCode.OK);

            Factory.DrainAuditQueue(); // persiste lo encolado (sin background writer en tests)

            var count = await Factory.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM gen_AuditLogs WHERE ClientUserAgent = 'aud56-probe' AND Servicio = 'Auditoria' AND ResultStatusCode = '200'");
            Assert.True(count >= 1, "el acceso al audit log deberia auto-auditarse (write-side)");
        }
    }
}
