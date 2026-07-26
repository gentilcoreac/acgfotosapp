using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Menus
{
    /// <summary>
    /// Menu (240) — BASELINE de aislamiento de runtime. Menu NO es multi-tenant (catalogo global del
    /// sistema, ABM root-only por authz); el aislamiento que importa es POR PERMISO y POR APLICACION:
    /// los endpoints de runtime (principal / allowed-routes) arman el menu del usuario desde el CONTEXTO
    /// autenticado (UserId + AplicacionId del header), no de un Id del body -> no hay patron de leak
    /// cross-tenant. El riesgo es de ESCALADA: ver/navegar opciones de permisos no asignados u otra app.
    /// allowed-routes es el mas critico: alimenta el route-guard del front (si filtra una ruta de un
    /// permiso ajeno, se accede por URL directa).
    ///
    /// Escenario sembrado: Menu A (PermisoRoot=1, app1, /granted), Menu B (Permiso 2 NO asignado, app1,
    /// /denied), Menu C (PermisoRoot=1, app2, /otra-app). userb recibe el rol1 (licenciado) -> Permiso 1.
    /// </summary>
    public class MenuIsolationTests : IntegrationTestBase
    {
        public MenuIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task SeedMenusYGrantAsync() => Factory.ExecuteSqlAsync(@"
            -- Permiso 2 (NO asignado a userb) y Aplicacion 2 (para el aislamiento por app).
            INSERT INTO gen_Permisos (Id, Nombre, CodigoPermiso, Descripcion, Activo, AplicacionId, EsRestringido)
                VALUES (2, 'P2', 'P2', 'd', true, 1, false);
            INSERT INTO gen_Aplicaciones (Id, Codigo, Nombre, Activo, Icono) VALUES (2, 'app2', 'App2', true, 'home');
            -- Menus activos y visibles.
            INSERT INTO gen_Menus (Nombre, Codigo, Estado, Orden, PermisoId, AplicacionId, VisibleSideMenu, VisibleDash, RoutePath) VALUES
                ('MenuA', 'MEN-A', true, 1, 1, 1, true, true, '/granted'),
                ('MenuB', 'MEN-B', true, 2, 2, 1, true, true, '/denied'),
                ('MenuC', 'MEN-C', true, 3, 1, 2, true, true, '/otra-app');
            -- userb -> rol1 (licenciado) -> Permiso 1 (PermisoRoot).
            INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);
            INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (10, 1, 2);
            INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, 1);");

        private async Task<HttpClient> UserBClientConAppAsync(string? aplicacionId = "1")
        {
            var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            if (aplicacionId != null)
            {
                client.DefaultRequestHeaders.Add("aplicacionId", aplicacionId);
            }
            return client;
        }

        [Fact] // MEN-25 (+MEN-24) — allowed-routes (route-guard) NO incluye rutas de permisos no asignados ni de otra app
        public async Task AllowedRoutes_solo_del_permiso_y_app_del_usuario()
        {
            await SeedMenusYGrantAsync();
            using var client = await UserBClientConAppAsync("1"); // app 1

            var resp = await client.GetAsync("/api/general/menus/allowed-routes");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("/granted", body);       // Permiso 1 (asignado), app 1
            Assert.DoesNotContain("/denied", body);   // Permiso 2 (NO asignado) -> no se filtra al guard
            Assert.DoesNotContain("/otra-app", body); // app 2 -> no se filtra a la app activa
        }

        [Fact] // MEN-09 (+MEN-08) — principal solo muestra menus de permisos asignados y de la app activa
        public async Task Principal_solo_del_permiso_y_app_del_usuario()
        {
            await SeedMenusYGrantAsync();
            using var client = await UserBClientConAppAsync("1");

            var resp = await client.GetAsync("/api/general/menus/principal");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("MenuA", body);
            Assert.DoesNotContain("MenuB", body); // permiso no asignado
            Assert.DoesNotContain("MenuC", body); // otra app
        }

        [Fact] // MEN-07/26 — no-root SIN aplicacion seleccionada: menu vacio (no arma cross-app)
        public async Task NoRoot_sin_aplicacion_devuelve_vacio()
        {
            await SeedMenusYGrantAsync();
            using var client = await UserBClientConAppAsync(aplicacionId: null); // sin header aplicacionId

            var resp = await client.GetAsync("/api/general/menus/allowed-routes");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var rutas = await resp.Content.ReadFromJsonAsync<List<object>>();
            Assert.Empty(rutas!);
        }

        [Fact] // MEN-01/23 — root@rootTenant arma su menu por PermisoRoot (no por roles), ve los menus de PermisoRoot
        public async Task Root_ve_menus_de_permiso_root()
        {
            await SeedMenusYGrantAsync();
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("aplicacionId", "1");

            var resp = await client.GetAsync("/api/general/menus/allowed-routes");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("/granted", body);     // Menu A: PermisoId=1 (PermisoRoot), app 1
            Assert.DoesNotContain("/denied", body); // Menu B: Permiso 2, no es PermisoRoot
            client.Dispose();
        }
    }
}
