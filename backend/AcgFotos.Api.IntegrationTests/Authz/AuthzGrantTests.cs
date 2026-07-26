using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Cadena de autorizacion CON permiso (AUTHZ-01) + discover (AUTHZ-11) + AllowAnonymous (AUTHZ-05).
    ///
    /// IMPORTANTE (responde "los permisos son dinamicos, ¿hay que actualizar los tests?"): este test NO
    /// depende del catalogo de permisos de produccion. Arma su PROPIO grafo de grant
    /// (rol licenciado -> permiso -> endpoint) sobre datos de test, y verifica que el MECANISMO concede el
    /// 200. Si manana se crean/renombran permisos en el producto, este test sigue valido: prueba la regla,
    /// no la configuracion concreta. El endpoint se cataloga via el discover real (misma firma, sin drift).
    /// </summary>
    public class AuthzGrantTests : AuthzTestBase
    {
        public AuthzGrantTests(AuthzWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUTHZ-11 — discover (root) repuebla el catalogo de endpoints
        public async Task Discover_como_root_repuebla_el_catalogo()
        {
            using var root = await CreateAuthenticatedClientAsync();

            var resp = await root.GetAsync("/api/general/discover");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var endpoints = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Endpoints WHERE Activo = true");
            Assert.True(endpoints > 0, "discover deberia dejar endpoints activos en el catalogo");
        }

        [Fact] // AUTHZ-01 — no-root CON la cadena rol(licenciado)->permiso->endpoint accede OK
        public async Task NoRoot_con_permiso_mapeado_accede_OK()
        {
            // 1) Catalogar los endpoints reales (firma correcta, sin drift) via discover de root.
            var endpointId = await DiscoverEndpointIdAsync("api/general/usuarios/mi-perfil", "GetPerfil");

            // 2) Armar el grafo de grant para userb (rol 1, permitido por su licencia TLI=1):
            //    licencia->rol, usuario->rol (directo), rol->permiso 1, permiso 1->endpoint.
            await Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);
                INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (10, 1, 2);
                INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, 1);
                INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId) VALUES (1, {endpointId});");

            // 3) userb ahora SI tiene permiso mapeado al endpoint.
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var resp = await client.GetAsync("/api/general/usuarios/mi-perfil");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTHZ-12 — discover por no-root con authz on (verifica 401 del catalogo vs 403 del filtro)
        public async Task Discover_por_no_root_es_denegado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.GetAsync("/api/general/discover");

            // El catalogo dice 401 (guarda interna de HomeController). Pero HomeController es ApiControllerBase:
            // el filtro EndpointAuthoritation deniega ANTES de llegar a la accion -> 403 (post ADR-0007).
            await resp.ShouldBeStatus(HttpStatusCode.Forbidden);
            var count = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Endpoints");
            Assert.Equal(0, count); // no se ejecuto el discover
        }

        [Fact] // AUTHZ-05 — endpoint [AllowAnonymous] no evalua permisos aunque authz este on
        public async Task Endpoint_AllowAnonymous_no_evalua_permisos()
        {
            using var client = CreateClient(); // sin token

            var resp = await client.GetAsync("/api/general/version");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        /// <summary>Cataloga los endpoints (discover root) y devuelve el ID del endpoint pedido.</summary>
        private async Task<long> DiscoverEndpointIdAsync(string route, string actionName)
        {
            using (var root = await CreateAuthenticatedClientAsync())
            {
                var resp = await root.GetAsync("/api/general/discover");
                await resp.ShouldBeStatus(HttpStatusCode.OK);
            }

            var id = await Factory.QueryScalarAsync<long>(
                $"SELECT ID FROM gen_Endpoints WHERE Route = '{route}' AND ActionName = '{actionName}'");
            Assert.True(id > 0, $"discover no registro el endpoint {actionName} ({route})");
            return id;
        }
    }
}
