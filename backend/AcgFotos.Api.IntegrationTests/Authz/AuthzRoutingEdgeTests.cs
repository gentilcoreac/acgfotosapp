using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Bordes de ruteo vs autorizacion (AUTHZ-57/58/59) con authz ON y un no-root: el ruteo resuelve
    /// ANTES que el filtro, asi que una ruta inexistente da 404 y un verbo no mapeado 405 (no 403); y la
    /// firma de cada endpoint incluye el HttpMethod, asi que un permiso de GET NO habilita el DELETE de la
    /// misma ruta. Previene enmascarar 404/405 como problemas de permiso y la escalada GET -> DELETE.
    /// </summary>
    public class AuthzRoutingEdgeTests : AuthzTestBase
    {
        public AuthzRoutingEdgeTests(AuthzWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUTHZ-57 — ruta que no matchea ninguna accion -> 404 (no entra al filtro), no 403
        public async Task Ruta_inexistente_da_404_no_403()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            // 'abc' no cumple el constraint {id:int} -> no hay action que matchee.
            var resp = await client.GetAsync("/api/general/endpoints/abc");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // AUTHZ-58 — verbo no permitido en una ruta valida -> 405 (resuelto por ruteo), no 403
        public async Task Verbo_no_permitido_da_405_no_403()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            // No hay PUT sobre /endpoints (el alta/edicion es POST .../update).
            var resp = await client.PutAsync("/api/general/endpoints", content: null);

            await resp.ShouldBeStatus(HttpStatusCode.MethodNotAllowed);
        }

        [Fact] // AUTHZ-59 — DELETE exige su propia firma: el permiso de GET .../{id:int} NO habilita el DELETE
        public async Task Permiso_de_GET_no_habilita_DELETE_en_la_misma_ruta()
        {
            // 1) Catalogar endpoints reales (firmas correctas) via discover de root.
            using (var root = await CreateAuthenticatedClientAsync())
                Assert.Equal(HttpStatusCode.OK, (await root.GetAsync("/api/general/discover")).StatusCode);

            var getEndpointId = await Factory.QueryScalarAsync<long>(
                "SELECT Id FROM gen_Endpoints WHERE Route = 'api/general/endpoints/{id:int}' AND HttpMethod = 'GET'");
            Assert.True(getEndpointId > 0, "discover deberia haber catalogado el GET .../{id:int}");

            // 2) Grant a userb SOLO de la firma GET .../{id:int} (rol licenciado -> permiso -> ese endpoint).
            await Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);
                INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (10, 1, 2);
                INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, 1);
                INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId) VALUES (1, {getEndpointId});");

            // Un id real cualquiera para la ruta (el filtro chequea la firma, no la existencia del recurso).
            var algunId = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Endpoints WHERE Activo = true LIMIT 1");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            // GET pasa (firma concedida).
            var get = await client.GetAsync($"/api/general/endpoints/{algunId}");
            await get.ShouldBeStatus(HttpStatusCode.OK);

            // DELETE en la MISMA ruta -> 403: su firma (DELETE) no esta en el set.
            var del = await client.DeleteAsync($"/api/general/endpoints/{algunId}");
            await del.ShouldBeStatus(HttpStatusCode.Forbidden);
        }
    }
}
