using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Endpoint dedicado POST /{id}/set-administrador (USR-47/49/50/52/53). El flag Administrador NO se
    /// expone en el UsuarioInputDto (defensa mass-assignment); se cambia acá, gateado a root/admin.
    /// </summary>
    public class UsuarioSetAdminTests : IntegrationTestBase
    {
        public UsuarioSetAdminTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // USR-47 — admin del tenant promueve a un usuario
        public async Task SetAdministrador_promueve()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin del tenant 2

            var resp = await client.PostAsJsonAsync(
                $"/api/general/usuarios/{TestData.UserB2Id}/set-administrador", new { administrador = true });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var admin = await Factory.QueryScalarAsync<int>($"SELECT Administrador FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal(1, admin);
        }

        [Fact] // USR-49 — usuario común (no root, no admin) NO puede: gate runtime
        public async Task SetAdministrador_por_usuario_comun_es_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-admin

            var resp = await client.PostAsJsonAsync(
                $"/api/general/usuarios/{TestData.UserB2Id}/set-administrador", new { administrador = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            var admin = await Factory.QueryScalarAsync<int>($"SELECT Administrador FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal(0, admin); // no se promovio
        }

        [Fact] // USR-50 — id inexistente
        public async Task SetAdministrador_id_inexistente_devuelve_400()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var resp = await client.PostAsJsonAsync(
                "/api/general/usuarios/999999/set-administrador", new { administrador = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotFound);
        }

        [Fact] // USR-52 — no toca otros campos (solo Administrador)
        public async Task SetAdministrador_no_toca_otros_campos()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            await client.PostAsJsonAsync(
                $"/api/general/usuarios/{TestData.UserB2Id}/set-administrador", new { administrador = true });

            var row = await Factory.QueryScalarAsync<string>(
                $"SELECT CONCAT(Administrador, '|', Nombre, '|', Email, '|', TenantId) FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal("1|User|userb2@tech-bi.com|2", row); // solo Administrador cambio
        }

        [Fact] // USR-53 — sin token el endpoint sensible no opera (no promueve)
        public async Task SetAdministrador_sin_token_no_opera()
        {
            using var client = CreateClient(); // sin Authorization

            var resp = await client.PostAsJsonAsync(
                $"/api/general/usuarios/{TestData.UserB2Id}/set-administrador", new { administrador = true });

            // No expone [Authorize], pero el gate runtime (IsRoot/IsAdmin=false para anonimo) lo corta.
            Assert.True(resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest,
                $"esperaba 401/400 pero fue {(int)resp.StatusCode}");
            var admin = await Factory.QueryScalarAsync<int>($"SELECT Administrador FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal(0, admin); // no se promovio
        }
    }
}
