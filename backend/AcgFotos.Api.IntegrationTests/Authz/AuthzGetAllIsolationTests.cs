using Microsoft.Extensions.DependencyInjection;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// SecurityRepository.GetAll (AUTHZ-28/29): el set de endpoints que arma el filtro de authz. Sale por
    /// SQL CRUDO (ADO), NO por EF -> el filtro global de tenant NO lo cubre, por eso necesita su propio
    /// test de aislamiento (caveat de SQL crudo, ver testing-convenciones-api.md). Se prueba a nivel
    /// componente: se resuelve ISecurityRepository del contenedor real y se consulta la base de tests.
    ///
    /// HALLAZGO #11: el catalogo plantea el escenario "mismo UserName en dos tenants distintos", pero
    /// gen_Usuarios tiene UNIQUE GLOBAL en UserName (UsuarioEFConfig: HasIndex(UserName).IsUnique(), sin
    /// TenantId) -> ese escenario NO es alcanzable. El aislamiento igual existe y se verifica por la via
    /// TENANT-MISMATCH: pedir el set con un TenantId que no es el del usuario devuelve VACIO. Es defensa
    /// en profundidad: si llegara un token con UserName valido pero TenantId forjado, no se filtra ningun
    /// permiso (el AND u.TenantId = @TenantId exige que el par (UserName, TenantId) sea consistente).
    /// </summary>
    public class AuthzGetAllIsolationTests : IntegrationTestBase
    {
        public AuthzGetAllIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        /// <summary>Arma el grafo de grant de userb (id 10, tenant 2) sobre un endpoint sintetico Activo=1.</summary>
        private Task SeedGrantForUserBAsync() => Factory.ExecuteSqlAsync(@"
            INSERT INTO gen_Endpoints (ActionName, ControllerName, Namespace, ModuleName, Route, HttpMethod, Activo)
                VALUES ('X','C','N','M','api/test/x','GET',true);
            INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);
            INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (10, 1, 2);
            INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, 1);
            INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId)
                VALUES (1, (SELECT Id FROM gen_Endpoints WHERE Route = 'api/test/x' AND HttpMethod = 'GET'));");

        private List<EndpointDto> GetAll(string userName, long tenantId)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISecurityRepository>();
            return repo.GetAll(userName, tenantId);
        }

        [Fact] // AUTHZ-28 — el set se filtra por (UserName, TenantId): con el tenant equivocado -> vacio
        public async Task GetAll_aisla_por_tenant()
        {
            await SeedGrantForUserBAsync();

            var enSuTenant = GetAll(TestData.UserB, TestData.ActiveTenantId);     // tenant 2 (el suyo)
            var enOtroTenant = GetAll(TestData.UserB, TestData.InactiveTenantId); // tenant 3 (ajeno)

            Assert.Single(enSuTenant);  // userb SI ve su endpoint en su propio tenant
            Assert.Empty(enOtroTenant); // con otro tenant NO se filtra el permiso (aislamiento)
        }

        [Fact] // AUTHZ-29 — GetAll es parametrizado: un UserName malicioso no inyecta (devuelve vacio, no todo)
        public async Task GetAll_no_es_inyectable_por_userName()
        {
            await SeedGrantForUserBAsync(); // hay un grant real: una inyeccion exitosa lo devolveria

            var resultado = GetAll("x' OR '1'='1", TestData.ActiveTenantId);

            // Tratado como literal (@UserName) -> no matchea ningun usuario -> vacio (no devuelve el grant de userb).
            Assert.Empty(resultado);
        }
    }
}
