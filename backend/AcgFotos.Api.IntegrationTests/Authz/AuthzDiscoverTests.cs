using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Mantenimiento del catalogo (discover) — AUTHZ-14/15/16. Discover es root-only con auth on; corre
    /// contra gen_Endpoints (entidad GLOBAL, sin TenantId) via SQL crudo del SecurityRepository. La
    /// identidad de un endpoint es (Route, HttpMethod): el RegisterEndpoints apaga TODO (DisableEndpoints)
    /// y luego INSERTA los nuevos o ACTUALIZA los existentes por esa identidad.
    /// </summary>
    public class AuthzDiscoverTests : AuthzTestBase
    {
        public AuthzDiscoverTests(AuthzWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUTHZ-14 — discover apaga Activo de TODOS y solo re-activa los descriptores presentes (gotcha)
        public async Task Discover_resetea_activo_y_no_reactiva_endpoints_retirados()
        {
            // Endpoint "fantasma" que no corresponde a ninguna ruta real, marcado Activo=1 (como si fuera
            // un endpoint retirado del codigo que quedo en el catalogo).
            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Endpoints (ActionName, ControllerName, Namespace, ModuleName, Route, HttpMethod, Activo) " +
                "VALUES ('Fantasma','Ghost','N','M','api/ruta/que/no/existe','GET',1)");

            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.GetAsync("/api/general/discover");
            await resp.ShouldBeStatus(HttpStatusCode.OK);

            // El fantasma quedo Activo=0: DisableEndpoints lo apago y ningun descriptor lo re-activo.
            var fantasmaActivo = await Factory.QueryScalarAsync<int>(
                "SELECT Activo FROM gen_Endpoints WHERE Route = 'api/ruta/que/no/existe' AND HttpMethod = 'GET'");
            Assert.Equal(0, fantasmaActivo);

            // Los endpoints REALES quedaron Activo=1.
            var realesActivos = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Endpoints WHERE Activo = 1");
            Assert.True(realesActivos > 0, "discover deberia dejar los endpoints reales Activo=1");
        }

        [Fact] // AUTHZ-15 — discover idempotente: dos corridas dan el mismo set, sin duplicados por identidad
        public async Task Discover_es_idempotente()
        {
            using var root = await CreateAuthenticatedClientAsync();

            Assert.Equal(HttpStatusCode.OK, (await root.GetAsync("/api/general/discover")).StatusCode);
            var activosTras1 = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Endpoints WHERE Activo = 1");

            Assert.Equal(HttpStatusCode.OK, (await root.GetAsync("/api/general/discover")).StatusCode);
            var activosTras2 = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Endpoints WHERE Activo = 1");

            Assert.Equal(activosTras1, activosTras2); // misma cantidad de activos
            // Sin filas duplicadas para una misma identidad (Route, HttpMethod): fue UPDATE, no INSERT.
            var duplicados = await Factory.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM (SELECT Route, HttpMethod FROM gen_Endpoints GROUP BY Route, HttpMethod HAVING COUNT(*) > 1) d");
            Assert.Equal(0, duplicados);
        }

        [Fact] // AUTHZ-16 — rename de controller: misma (Route, HttpMethod) -> UPDATE de metadata, sin INSERT nuevo
        public async Task Discover_actualiza_metadata_ante_rename_sin_duplicar()
        {
            // Fila pre-existente para una ruta REAL pero con ControllerName viejo y Activo=0 (como tras un rename).
            const string route = "api/general/usuarios/mi-perfil";
            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Endpoints (ActionName, ControllerName, Namespace, ModuleName, Route, HttpMethod, Activo) " +
                $"VALUES ('GetPerfil','NombreViejoController','N','M','{route}','GET',0)");

            using var root = await CreateAuthenticatedClientAsync();
            Assert.Equal(HttpStatusCode.OK, (await root.GetAsync("/api/general/discover")).StatusCode);

            // Sigue habiendo UNA sola fila para esa identidad: fue UPDATE, no un INSERT duplicado.
            var filas = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_Endpoints WHERE Route = '{route}' AND HttpMethod = 'GET'");
            Assert.Equal(1, filas);

            // La metadata quedo sincronizada (ya no tiene el ControllerName viejo) y Activo=1.
            var conNombreViejo = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_Endpoints WHERE Route = '{route}' AND HttpMethod = 'GET' AND ControllerName = 'NombreViejoController'");
            Assert.Equal(0, conNombreViejo);
            var activo = await Factory.QueryScalarAsync<int>(
                $"SELECT Activo FROM gen_Endpoints WHERE Route = '{route}' AND HttpMethod = 'GET'");
            Assert.Equal(1, activo);
        }
    }
}
