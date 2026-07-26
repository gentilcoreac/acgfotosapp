using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Edición de usuario y defensa de mass-assignment (USR-02/10/11/12/13). El UsuarioInputDto NO expone
    /// los campos sensibles (Administrador, TenantId, EmailConfirmed, SecurityStamp) y UserName es
    /// inmutable: aunque el JSON crudo los incluya, no deben tener efecto en DB.
    /// </summary>
    public class UsuarioEditTests : IntegrationTestBase
    {
        public UsuarioEditTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // USR-02 — edición conserva los campos sensibles (solo cambia lo editable)
        public async Task Edicion_conserva_campos_sensibles()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "userb2",
                nombre = "NombreNuevo",
                apellido = "ApellidoNuevo",
                email = "userb2@tech-bi.com",
                telefono = 1122334499L,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            await resp.ShouldBeOk();
            var row = await Factory.QueryScalarAsync<string>(
                $"SELECT CONCAT(Nombre, '|', Administrador, '|', EmailConfirmed, '|', TenantId, '|', UserName) FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal("NombreNuevo|f|t|2|userb2", row); // nombre cambia; Administrador=false/EmailConfirmed=true/Tenant=2/UserName intactos (Postgres: bool->text es 't'/'f')
        }

        [Fact] // USR-10 — "administrador":true en el body crudo NO promueve al usuario
        public async Task MassAssignment_Administrador_ignorado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "userb2",
                nombre = "X",
                apellido = "Y",
                email = "userb2@tech-bi.com",
                telefono = 1122334499L,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
                administrador = true, // <- campo fuera del InputDto
            });

            var admin = await Factory.QueryScalarAsync<int>($"SELECT Administrador FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal(0, admin); // sigue no-admin
        }

        [Fact] // USR-11 — "tenantId":<otro> en el body crudo NO mueve de tenant
        public async Task MassAssignment_TenantId_ignorado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "userb2",
                nombre = "X",
                apellido = "Y",
                email = "userb2@tech-bi.com",
                telefono = 1122334499L,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
                tenantId = TestData.InactiveTenantId, // <- intento mover a tenant 3
            });

            var tenant = await Factory.QueryScalarAsync<long>($"SELECT TenantId FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal(TestData.ActiveTenantId, tenant); // sigue en tenant 2
        }

        [Fact] // USR-12 — "emailConfirmed":true en el body crudo NO auto-confirma
        public async Task MassAssignment_EmailConfirmed_ignorado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            // pending (13) tiene EmailConfirmed=0; el intento de confirmarlo por body no debe prosperar.
            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.PendingId,
                userName = "pending",
                nombre = "X",
                apellido = "Y",
                email = "pending@tech-bi.com",
                telefono = 1122334488L,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
                emailConfirmed = true, // <- fuera del InputDto
            });

            var confirmed = await Factory.QueryScalarAsync<int>($"SELECT EmailConfirmed FROM gen_Usuarios WHERE Id = {TestData.PendingId}");
            Assert.Equal(0, confirmed); // sigue sin confirmar
        }

        [Fact] // USR-13 — UserName es inmutable en edición (se restaura desde la entidad)
        public async Task UserName_inmutable_en_edicion()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "hacker", // <- intento de cambiar el login
                nombre = "X",
                apellido = "Y",
                email = "userb2@tech-bi.com",
                telefono = 1122334499L,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            var userName = await Factory.QueryScalarAsync<string>($"SELECT UserName FROM gen_Usuarios WHERE Id = {TestData.UserB2Id}");
            Assert.Equal("userb2", userName); // sin cambiar
        }

        [Fact] // USR-22 — update con Bloqueado=true setea LockoutEnd (ADR-0006)
        public async Task Update_con_Bloqueado_true_setea_lockout()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "userb2",
                nombre = "User",
                apellido = "B2",
                email = "userb2@tech-bi.com",
                telefono = 1122334499L,
                bloqueado = true,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            var locked = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_Usuarios WHERE Id = {TestData.UserB2Id} AND LockoutEnd IS NOT NULL");
            Assert.Equal(1, locked);
        }

        [Fact] // USR-23 — update con Bloqueado=false limpia LockoutEnd
        public async Task Update_con_Bloqueado_false_limpia_lockout()
        {
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Usuarios SET LockoutEnd = now() + interval '1 year' WHERE Id = {TestData.UserB2Id}");
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "userb2",
                nombre = "User",
                apellido = "B2",
                email = "userb2@tech-bi.com",
                telefono = 1122334499L,
                bloqueado = false,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            var locked = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_Usuarios WHERE Id = {TestData.UserB2Id} AND LockoutEnd IS NOT NULL");
            Assert.Equal(0, locked);
        }
    }
}
