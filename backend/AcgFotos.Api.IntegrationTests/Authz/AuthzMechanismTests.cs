using System.Net;
using System.Net.Http.Headers;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Mecanismo de autorizacion por endpoint (EndpointAuthoritation) con AuthorizationEnabled=true —
    /// AUTHZ-02/03/04. Son los INVARIANTES del mecanismo, independientes del catalogo dinamico de
    /// permisos: deny-by-default para no-root sin permiso, bypass de root, y 401 de autenticacion.
    /// (AUTHZ-01 "con permiso -> 200" se cubre aparte sembrando el grant via discovery.)
    /// </summary>
    public class AuthzMechanismTests : AuthzTestBase
    {
        public AuthzMechanismTests(AuthzWebApplicationFactory factory) : base(factory) { }

        // Endpoint protegido, parametrico-libre y barato (devuelve el perfil del usuario actual).
        private const string ProtectedEndpoint = "/api/general/usuarios/mi-perfil";

        [Fact] // AUTHZ-02 — deny-by-default: no-root sin permiso mapeado -> 403 (ApiErrorResponse)
        public async Task NoRoot_sin_permiso_recibe_403()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.GetAsync(ProtectedEndpoint);

            await resp.ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
        }

        [Fact] // AUTHZ-03 — root real bypassa authz aunque no tenga permisos
        public async Task Root_bypassa_authz()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync(ProtectedEndpoint);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTHZ-04 — sin token: 401 de autenticacion (pre-MVC), no 403
        public async Task Sin_token_devuelve_401()
        {
            using var client = CreateClient();

            var resp = await client.GetAsync(ProtectedEndpoint);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTHZ-10 — token con firma/formato invalido: 401 de autenticacion (pre-MVC), no se audita
        public async Task Token_invalido_devuelve_401()
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "no.es.un-jwt-valido");

            var resp = await client.GetAsync(ProtectedEndpoint);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }
    }
}
