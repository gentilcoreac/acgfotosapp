using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Sincronización de la colección de roles del usuario (USR-17/18) vía ChildCollectionSync.SyncBy:
    /// agregar y quitar roles; los hijos heredan UsuarioId/TenantId de la entidad.
    /// </summary>
    public class UsuarioRoleSyncTests : IntegrationTestBase
    {
        public UsuarioRoleSyncTests(TestWebApplicationFactory factory) : base(factory) { }

        private static object EditWithRoles(long[] rolIds) => new
        {
            id = TestData.UserB2Id,
            userName = "userb2",
            nombre = "User",
            apellido = "B2",
            email = "userb2@tech-bi.com",
            telefono = 1122334499L,
            roles = rolIds.Select(r => new { rolId = r }).ToArray(),
            usuarioAplicaciones = Array.Empty<object>(),
        };

        [Fact] // USR-17 — alta de rol: se agrega el UsuarioRol (hijo con UsuarioId/TenantId heredados)
        public async Task Sync_de_roles_agrega()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", EditWithRoles(new long[] { 1 }));

            await resp.ShouldBeOk();
            var row = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_UsuarioRoles WHERE UsuarioId = {TestData.UserB2Id} AND RolId = 1 AND TenantId = {TestData.ActiveTenantId}");
            Assert.Equal(1, row);
        }

        [Fact] // USR-18 — baja de rol: se elimina el UsuarioRol que ya no viene
        public async Task Sync_de_roles_quita()
        {
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_UsuarioRoles (UsuarioId, RolId, TenantId) VALUES ({TestData.UserB2Id}, 1, {TestData.ActiveTenantId})");
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", EditWithRoles(Array.Empty<long>()));

            await resp.ShouldBeOk();
            var row = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_UsuarioRoles WHERE UsuarioId = {TestData.UserB2Id} AND RolId = 1");
            Assert.Equal(0, row);
        }

        [Fact] // USR-20 — sync de apps: el alta de una UsuarioAplicacion via API (wiring de la colección,
        // distinto al de roles: otra entidad/EF-config/call-site, aunque comparta SyncBy)
        public async Task Sync_de_apps_agrega()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(id: TestData.UserB2Id, userName: "userb2", apellido: "B2", telefono: 1122334499L,
                    apps: new[] { Payloads.App(1) }));

            await resp.ShouldBeOk();
            var row = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_UsuarioAplicaciones WHERE UsuarioId = {TestData.UserB2Id} AND AplicacionId = 1 AND TenantId = {TestData.ActiveTenantId}");
            Assert.Equal(1, row); // hijo creado con TenantId heredado
        }

        // NOTA: la BAJA de app reusa el SyncBy genérico ya probado por USR-18 (baja de rol) → no se
        // reimplementa. USR-21 sí: el flag Default se actualiza en un loop POST-sync aunque el set no cambie.
        [Fact] // USR-21 — actualizar el flag Default de una app sin cambiar el set
        public async Task Sync_de_apps_actualiza_flag_default()
        {
            // userb2 ya tiene la app 1 con Default=false.
            await Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_UsuarioAplicaciones (UsuarioId, AplicacionId, \"default\", TenantId) VALUES ({TestData.UserB2Id}, 1, false, {TestData.ActiveTenantId})");
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            // Mismo set (app 1) pero ahora Default=true.
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(id: TestData.UserB2Id, userName: "userb2", apellido: "B2", telefono: 1122334499L,
                    apps: new[] { Payloads.App(1, @default: true) }));

            await resp.ShouldBeOk();
            var def = await Factory.QueryScalarAsync<int>(
                $"SELECT \"default\" FROM gen_UsuarioAplicaciones WHERE UsuarioId = {TestData.UserB2Id} AND AplicacionId = 1");
            Assert.Equal(1, def); // Default actualizado por el loop post-sync
        }
    }
}
