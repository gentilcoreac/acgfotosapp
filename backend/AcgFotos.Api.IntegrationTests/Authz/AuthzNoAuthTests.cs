using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// AUTHZ-07/13 — con AuthenticationEnabled=false el Startup NO registra UseAuthentication/UseAuthorization
    /// y el filtro EndpointAuthoritation corta en seco (authIsEnabled && checkPermissions = false). Previene
    /// acoplar authz a auth al reves (que el chequeo de permisos corra con la auth apagada). Host EFIMERO con
    /// la auth apagada (mismo patron que los tests de rate-limit), sin token: el caso de dev local sin tokens.
    /// </summary>
    public class AuthzNoAuthTests : IntegrationTestBase
    {
        public AuthzNoAuthTests(TestWebApplicationFactory factory) : base(factory) { }

        private WebApplicationFactory<Program> NoAuth() =>
            Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AuthenticationEnabled"] = "false",
                })));

        [Fact] // AUTHZ-07 — auth off: un endpoint securizado responde 200 sin token (no se evalua authz)
        public async Task Con_auth_off_endpoint_securizado_pasa_sin_token()
        {
            using var factory = NoAuth();
            using var client = factory.CreateClient(); // sin header Authorization

            var resp = await client.GetAsync("/api/general/endpoints");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTHZ-13 — auth off: discover abierto (bootstrap del catalogo en dev local sin tokens)
        public async Task Con_auth_off_discover_abierto_sin_token()
        {
            using var factory = NoAuth();
            using var client = factory.CreateClient();

            var resp = await client.GetAsync("/api/general/discover");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var activos = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Endpoints WHERE Activo = true");
            Assert.True(activos > 0, "discover sin auth deberia poblar el catalogo");
        }
    }
}
