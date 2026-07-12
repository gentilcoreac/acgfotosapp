using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// AUTHZ-08 — el deny por autorizacion se AUDITA. El short-circuit de EndpointAuthoritation (devuelve
    /// 403 desde un IAuthorizationFilter) saltea los result filters, incluido el AuditLogFilter; por eso el
    /// propio filtro encola el evento (AuditDenied, ADR-0005). Es el evento de seguridad mas valioso del
    /// log. Determinístico: tras la request el evento ya esta encolado -> se drena a mano (DrainAuditQueue).
    /// (Estaba diferido del area 130 por necesitar la infra de drenado; ahora se cubre.)
    /// </summary>
    public class AuthzAuditDenyTests : AuthzTestBase
    {
        public AuthzAuditDenyTests(AuthzWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUTHZ-08 — un 403 de authz queda auditado con UsuarioId + status 403
        public async Task Deny_de_authz_se_audita()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root sin permiso
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "authz08-probe");

            var resp = await client.GetAsync("/api/general/endpoints");
            await resp.ShouldBeStatus(HttpStatusCode.Forbidden);

            Factory.DrainAuditQueue(); // persiste lo encolado por AuditDenied (sin background writer en tests)

            // AuditDenied registro el 403 (no el AuditLogFilter, que el short-circuit saltea).
            var count = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_AuditLogs WHERE ClientUserAgent = 'authz08-probe' AND ResultStatusCode = '403' AND UsuarioId = {TestData.UserBId}");
            Assert.True(count >= 1, "el deny de authz deberia auditarse (AuditDenied)");
        }
    }
}
