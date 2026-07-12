using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Cadena de escritura de authz (Permiso/Rol) — AUTHZ-43/44/45/47/48/51/53/54. Es la fuente que
    /// ALIMENTA la decision de autorizacion (PermisoEndpoint / RolPermiso) + los flags sensibles
    /// (EsRestringido, EsDefaultParaNuevoTenant). Lo que se prueba (preservacion anti mass-assignment,
    /// guard isRoot server-side, sync de colecciones) es INDEPENDIENTE del filtro de endpoint -> corre en
    /// el host base (authz off). Justamente 45/53 valen porque el guard isRoot protege AUNQUE el filtro
    /// de endpoint este apagado (no se confia solo en el endpoint). Permiso/Rol son entidades GLOBALES:
    /// root (en su tenant raiz) las edita sin el guard multi-tenant.
    /// </summary>
    public class AuthzWriteChainTests : IntegrationTestBase
    {
        public AuthzWriteChainTests(TestWebApplicationFactory factory) : base(factory) { }

        // Cuerpo de update de Permiso (id 1, el del seed). esRestringido viaja SIEMPRE: el DTO debe
        // ignorarlo (no existe en PermisoInputDto) — es el intento de mass-assignment.
        private static object PermisoBody(long[]? endpoints = null, bool esRestringidoMassAssign = false) => new
        {
            id = 1,
            nombre = "PermisoRoot",
            codigoPermiso = "PermisoRoot",
            descripcion = "Permiso root",
            activo = true,
            aplicacionId = 1,
            endpoints = (endpoints ?? System.Array.Empty<long>()).Select(e => new { endpointId = e }).ToArray(),
            esRestringido = esRestringidoMassAssign,
        };

        private async Task<long[]> SeedEndpointsAsync(int n)
        {
            var ids = new long[n];
            for (var i = 0; i < n; i++)
            {
                await Factory.ExecuteSqlAsync(
                    "INSERT INTO gen_Endpoints (ActionName, ControllerName, Namespace, ModuleName, Route, HttpMethod, Activo) " +
                    $"VALUES ('A{i}','C','N','M','api/test/e{i}','GET',1)");
                ids[i] = await Factory.QueryScalarAsync<long>(
                    $"SELECT Id FROM gen_Endpoints WHERE Route = 'api/test/e{i}' AND HttpMethod = 'GET'");
            }
            return ids;
        }

        [Fact] // AUTHZ-54 — los InputDto NO exponen los flags sensibles (defensa de mass-assignment a nivel de tipo)
        public void DTOs_no_exponen_flags_sensibles()
        {
            Assert.Null(typeof(PermisoInputDto).GetProperty("EsRestringido"));
            Assert.Null(typeof(RolInputDto).GetProperty("EsDefaultParaNuevoTenant"));
        }

        [Fact] // AUTHZ-43 — Update de Permiso preserva EsRestringido (ApplyInput lo ignora)
        public async Task Update_de_permiso_preserva_EsRestringido()
        {
            // Seed: Permiso 1 con EsRestringido=1. El body intenta bajarlo a false (mass-assignment).
            var pre = await Factory.QueryScalarAsync<bool>("SELECT EsRestringido FROM gen_Permisos WHERE Id = 1");
            Assert.True(pre); // precondicion

            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/permisos/update",
                PermisoBody(esRestringidoMassAssign: false));

            await resp.ShouldBeOk();
            var post = await Factory.QueryScalarAsync<bool>("SELECT EsRestringido FROM gen_Permisos WHERE Id = 1");
            Assert.True(post); // sigue restringido: el campo del body se ignoro
        }

        [Fact] // AUTHZ-44 — set-es-restringido como root cambia el flag
        public async Task SetEsRestringido_como_root_cambia_el_flag()
        {
            await Factory.ExecuteSqlAsync("UPDATE gen_Permisos SET EsRestringido = 0 WHERE Id = 1");

            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/permisos/1/set-es-restringido", new { esRestringido = true });

            await resp.ShouldBeOk();
            var flag = await Factory.QueryScalarAsync<bool>("SELECT EsRestringido FROM gen_Permisos WHERE Id = 1");
            Assert.True(flag);
        }

        [Fact] // AUTHZ-45 — set-es-restringido rechazado a no-root (guard isRoot server-side, no solo el filtro)
        public async Task SetEsRestringido_rechazado_a_no_root()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.PostAsJsonAsync("/api/general/permisos/1/set-es-restringido", new { esRestringido = false });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            var flag = await Factory.QueryScalarAsync<bool>("SELECT EsRestringido FROM gen_Permisos WHERE Id = 1");
            Assert.True(flag); // sigue en el valor del seed (1), sin cambios
        }

        [Fact] // AUTHZ-47 — Update de Permiso sincroniza Endpoints: alta de hijos
        public async Task Update_de_permiso_agrega_endpoints()
        {
            var e = await SeedEndpointsAsync(3);
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId) VALUES (1, {e[0]})");

            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/permisos/update",
                PermisoBody(endpoints: new[] { e[0], e[1], e[2] }));

            await resp.ShouldBeOk();
            var count = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = 1");
            Assert.Equal(3, count);
        }

        [Fact] // AUTHZ-48 — Update de Permiso sincroniza Endpoints: baja de hijos (sin orphans)
        public async Task Update_de_permiso_quita_endpoints()
        {
            var e = await SeedEndpointsAsync(3);
            await Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId) VALUES (1,{e[0]}),(1,{e[1]}),(1,{e[2]})");

            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/permisos/update",
                PermisoBody(endpoints: new[] { e[0] }));

            await resp.ShouldBeOk();
            var count = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = 1");
            Assert.Equal(1, count);
            var quedaE0 = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = 1 AND EndpointId = {e[0]}");
            Assert.Equal(1, quedaE0);
        }

        [Fact] // AUTHZ-51 — Update de Rol sincroniza PermisoIds via ChildCollectionSync (distinto wiring que Permiso->Endpoints)
        public async Task Update_de_rol_sincroniza_permisos()
        {
            // Permisos 2 y 3 (globales) + Rol 1 con RolPermisos {1,2}.
            await Factory.ExecuteSqlAsync(@"
                SET IDENTITY_INSERT gen_Permisos ON;
                INSERT INTO gen_Permisos (Id, Nombre, CodigoPermiso, Descripcion, Activo, AplicacionId, EsRestringido)
                    VALUES (2,'P2','P2','d',1,1,0),(3,'P3','P3','d',1,1,0);
                SET IDENTITY_INSERT gen_Permisos OFF;
                INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1,1),(1,2);");

            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/roles/update",
                new { id = 1, descripcion = "Administrador", permisoIds = new[] { 2, 3 } });

            await resp.ShouldBeOk();
            var permisos = await Factory.QueryScalarAsync<string>(
                "SELECT STRING_AGG(CAST(PermisoId AS varchar), ',') WITHIN GROUP (ORDER BY PermisoId) FROM gen_RolPermisos WHERE RolId = 1");
            Assert.Equal("2,3", permisos); // quito p1, agrego p3, dejo p2
        }

        [Fact] // AUTHZ-53 — set-default-tenant rechazado a no-root (guard isRoot server-side)
        public async Task SetDefaultTenant_rechazado_a_no_root()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.PostAsJsonAsync("/api/general/roles/1/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            var flag = await Factory.QueryScalarAsync<bool>("SELECT EsDefaultParaNuevoTenant FROM gen_Roles WHERE Id = 1");
            Assert.False(flag); // seed = 0, sin cambios
        }
    }
}
