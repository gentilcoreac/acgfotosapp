using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Invalidacion del cache de authz por VERSION (AUTHZ-31/32) — el corazon del cache versionado
    /// (ADR-0003). La key del set de endpoints lleva la authzVersion; cuando se cambia un permiso, el
    /// bump (en AcgFotosDbContext.SaveChanges) sube la version => la key cambia => la proxima request
    /// recomputa con el cambio, SIN re-login. Claves del test: (a) el cambio de grant se hace por la
    /// **API** (pasa por EF -> dispara el bump); hacerlo por SQL crudo NO bumpea. (b) el usuario arranca
    /// con un set **NO vacio** (un set vacio no se cachea -> se reintenta solo y no probaria la invalidacion).
    /// </summary>
    public class AuthzCacheVersionTests : AuthzTestBase
    {
        public AuthzCacheVersionTests(AuthzWebApplicationFactory factory) : base(factory) { }

        /// <summary>Cataloga endpoints (discover root) y devuelve los ids de mi-perfil GET y endpoints-list GET.</summary>
        private async Task<(long miPerfil, long endpointsList)> DiscoverAsync()
        {
            using (var root = await CreateAuthenticatedClientAsync())
                Assert.Equal(HttpStatusCode.OK, (await root.GetAsync("/api/general/discover")).StatusCode);

            var miPerfil = await Factory.QueryScalarAsync<long>(
                "SELECT Id FROM gen_Endpoints WHERE Route = 'api/general/usuarios/mi-perfil' AND HttpMethod = 'GET'");
            var endpointsList = await Factory.QueryScalarAsync<long>(
                "SELECT Id FROM gen_Endpoints WHERE Route = 'api/general/endpoints' AND HttpMethod = 'GET'");
            Assert.True(miPerfil > 0 && endpointsList > 0, "discover deberia catalogar mi-perfil y el listado de endpoints");
            return (miPerfil, endpointsList);
        }

        /// <summary>Grant inicial por SQL (NO bumpea): userb(rol1 licenciado) -> permiso1 -> [endpointIds].</summary>
        private Task GrantAsync(params long[] endpointIds)
        {
            var inserts = string.Join("", endpointIds.Select(e =>
                $"INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId) VALUES (1, {e});"));
            return Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);
                INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (10, 1, 2);
                INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, 1);
                {inserts}");
        }

        /// <summary>Root re-sincroniza los endpoints del permiso 1 via API => pasa por EF => BUMP de version.</summary>
        private async Task SetPermisoEndpointsViaApiAsync(params long[] endpointIds)
        {
            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/permisos/update", new
            {
                id = 1,
                nombre = "PermisoRoot",
                codigoPermiso = "PermisoRoot",
                descripcion = "d",
                activo = true,
                aplicacionId = 1,
                endpoints = endpointIds.Select(e => new { endpointId = e }).ToArray(),
            });
            await resp.ShouldBeOk();
        }

        [Fact] // AUTHZ-31 — otorgar un permiso se ve al instante (el bump invalida el cache, sin re-login)
        public async Task Otorgar_permiso_se_ve_sin_relogin()
        {
            var (miPerfil, endpointsList) = await DiscoverAsync();
            await GrantAsync(miPerfil); // userb arranca SOLO con mi-perfil (set NO vacio => se cachea)

            using var userb = await CreateAuthenticatedClientAsync(TestData.UserB);

            // Cachea el set {miPerfil} en la version actual.
            Assert.Equal(HttpStatusCode.OK, (await userb.GetAsync("/api/general/usuarios/mi-perfil")).StatusCode);
            // El otro endpoint aun no esta en el set.
            Assert.Equal(HttpStatusCode.Forbidden, (await userb.GetAsync("/api/general/endpoints")).StatusCode);

            // Otorga el endpoint via API (bump de version).
            await SetPermisoEndpointsViaApiAsync(miPerfil, endpointsList);

            // Sin re-login: la key de cache cambio con la version => recomputa => ahora 200.
            Assert.Equal(HttpStatusCode.OK, (await userb.GetAsync("/api/general/endpoints")).StatusCode);
        }

        [Fact] // AUTHZ-32 — revocar un permiso corta el acceso al instante (bump), sin acceso residual
        public async Task Revocar_permiso_corta_acceso_sin_relogin()
        {
            var (miPerfil, endpointsList) = await DiscoverAsync();
            await GrantAsync(miPerfil, endpointsList); // userb arranca con AMBOS

            using var userb = await CreateAuthenticatedClientAsync(TestData.UserB);

            // Cachea el set {miPerfil, endpointsList}.
            Assert.Equal(HttpStatusCode.OK, (await userb.GetAsync("/api/general/endpoints")).StatusCode);

            // Revoca endpointsList (deja solo mi-perfil) via API (bump).
            await SetPermisoEndpointsViaApiAsync(miPerfil);

            // Sin re-login: acceso cortado (no hay residual por cache stale).
            Assert.Equal(HttpStatusCode.Forbidden, (await userb.GetAsync("/api/general/endpoints")).StatusCode);
        }
    }
}
