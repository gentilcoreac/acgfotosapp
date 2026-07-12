using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// AUTHZ-06 — con AuthorizationEnabled=false (default dev, el host BASE) el filtro no evalua firmas:
    /// un no-root sin permiso igual pasa. Confirma que el flag realmente gatea la autorizacion por
    /// endpoint (evita falsos 403 en dev cuando esta apagada).
    /// </summary>
    public class AuthzDisabledTests : IntegrationTestBase
    {
        public AuthzDisabledTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUTHZ-06
        public async Task Con_authz_off_no_root_sin_permiso_igual_pasa()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.GetAsync("/api/general/usuarios/mi-perfil");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }
    }
}
