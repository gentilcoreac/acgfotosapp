using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// GET /{id}/roles-efectivos (USR-54/56/57): proyecta la vista vw_UsuarioRolesEfectivos
    /// (roles directos ∪ de grupos + flag PermitidoPorLicencia). Distinto de todo lo demás (read-only,
    /// con la unión y el left-join a grupo).
    /// </summary>
    public class UsuarioRolesEfectivosTests : IntegrationTestBase
    {
        public UsuarioRolesEfectivosTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record RolEfectivo(long RolId, string Origen, long? GrupoId, string? GrupoNombre, bool PermitidoPorLicencia);

        private async Task<List<RolEfectivo>> GetRolesEfectivosAsync(long usuarioId)
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin del tenant 2
            var resp = await client.GetAsync($"/api/general/usuarios/{usuarioId}/roles-efectivos");
            await resp.ShouldBeStatus(HttpStatusCode.OK);
            return (await resp.Content.ReadFromJsonAsync<List<RolEfectivo>>())!;
        }

        [Fact] // USR-56 — solo roles directos: Origen=Directo, sin grupo (left join null)
        public async Task Solo_directos()
        {
            await Factory.ExecuteSqlAsync("INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (14, 1, 2)");

            var roles = await GetRolesEfectivosAsync(TestData.UserB2Id);

            var r = Assert.Single(roles);
            Assert.Equal(1, r.RolId);
            Assert.Equal("Directo", r.Origen);
            Assert.Null(r.GrupoId);
        }

        [Fact] // USR-57 — usuario sin roles ni grupos: lista vacía (no 500)
        public async Task Sin_roles_devuelve_lista_vacia()
        {
            var roles = await GetRolesEfectivosAsync(TestData.UserB2Id); // userb2 no tiene roles en el seed

            Assert.Empty(roles);
        }

        [Fact] // USR-54 — unión directos ∪ grupos (Origen Directo y Grupo con GrupoNombre)
        public async Task Directos_union_grupos()
        {
            // Rol directo 1.
            await Factory.ExecuteSqlAsync("INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES (14, 1, 2)");
            // Grupo (vía API) con rol 2, y userb2 como miembro.
            using (var admin = await CreateAuthenticatedClientAsync(TestData.AdminB2))
                await admin.PostAsJsonAsync("/api/general/grupos/update", Payloads.Grupo("g-roles"));
            var grupoId = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Grupos WHERE Nombre = 'g-roles'");
            // Rol 1 también para el grupo (el seed solo tiene Rol 1). La vista hace UNION ALL: el mismo
            // rol vía directo + grupo produce DOS filas con distinto Origen → prueba la unión.
            await Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_GrupoRoles (TenantId, GrupoId, RolId) VALUES (2, {grupoId}, 1);" +
                $"INSERT INTO gen_UsuarioGrupos (UsuarioId, GrupoId, TenantId) VALUES (14, {grupoId}, 2);");

            var roles = await GetRolesEfectivosAsync(TestData.UserB2Id);

            Assert.Contains(roles, r => r.RolId == 1 && r.Origen == "Directo" && r.GrupoId == null);
            Assert.Contains(roles, r => r.RolId == 1 && r.Origen == "Grupo" && r.GrupoNombre == "g-roles");
        }
    }
}
