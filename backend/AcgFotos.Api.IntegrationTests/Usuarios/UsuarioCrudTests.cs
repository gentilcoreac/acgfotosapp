using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Usuarios — validaciones de email, visibilidad por rol en GetById y baja (USR-05/14/15/31/32/43/44).
    /// El aislamiento multi-tenant de GetById/Search/Delete ya está cubierto en MultiTenant* (MT-01/02/03)
    /// → acá no se reimplementa (se evita duplicar).
    /// </summary>
    public class UsuarioCrudTests : IntegrationTestBase
    {
        public UsuarioCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private static object NewUser(string userName, string email) => new
        {
            userName,
            nombre = "N",
            apellido = "A",
            email,
            telefono = 1122334400L,
            roles = Array.Empty<object>(),
            usuarioAplicaciones = Array.Empty<object>(),
        };

        [Fact] // USR-14 — alta con email duplicado
        public async Task Alta_con_email_duplicado_devuelve_400()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", NewUser("nuevo-dup", "userb2@tech-bi.com"));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorEmailExists);
        }

        [Fact] // USR-15 — edición con email de OTRO usuario falla (excluye el propio)
        public async Task Edicion_con_email_de_otro_devuelve_400()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            // userb2 (14) intenta tomar el email de userb (10).
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.UserB2Id,
                userName = "userb2",
                nombre = "N",
                apellido = "A",
                email = "userb@tech-bi.com", // de userb
                telefono = 1122334499L,
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorEmailExists);
        }

        [Fact] // USR-05 — email inválido lo rechaza el validator
        public async Task Alta_con_email_invalido_devuelve_400()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", NewUser("nuevo-mail-malo", "no-es-mail"));

            var error = await resp.ShouldBeBadRequest(MessagesAPI.ErrorValidation); // BusinessValidationException con errores
            Assert.NotEmpty(error.Errors);                            // detalle del validator
        }

        [Fact] // USR-31 — un no-admin NO puede ver por Id a un admin
        public async Task NoAdmin_viendo_admin_es_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-admin, tenant 2

            var resp = await client.GetAsync($"/api/general/usuarios/{TestData.AdminB2Id}"); // adminb2 (admin)

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesViewUserSelected);
        }

        [Fact] // USR-32 — un no-admin SÍ puede ver por Id a otro no-admin
        public async Task NoAdmin_viendo_noAdmin_devuelve_200()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.GetAsync($"/api/general/usuarios/{TestData.UserB2Id}"); // userb2 (no-admin)

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // USR-43 — baja happy path
        public async Task Delete_happy_path()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin tenant 2

            var resp = await client.DeleteAsync($"/api/general/usuarios/{TestData.PendingId}"); // pending (sin dependencias)

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var count = await Factory.QueryScalarAsync<int>($"SELECT COUNT(*) FROM gen_Usuarios WHERE Id = {TestData.PendingId}");
            Assert.Equal(0, count);
        }

        [Fact] // USR-44 — baja de usuario inexistente
        public async Task Delete_inexistente_devuelve_400()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var resp = await client.DeleteAsync("/api/general/usuarios/999999");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotFound);
        }

        [Fact] // USR-45 — DELETE cross-tenant no borra el usuario ajeno (aislamiento en la baja)
        public async Task Delete_cross_tenant_no_borra_al_ajeno()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin tenant 2

            // root (Id=1) es del tenant 1: el filtro lo oculta -> no debe poder borrarlo.
            await client.DeleteAsync($"/api/general/usuarios/{TestData.RootId}");

            var sigue = await Factory.QueryScalarAsync<int>($"SELECT COUNT(*) FROM gen_Usuarios WHERE Id = {TestData.RootId}");
            Assert.Equal(1, sigue); // root intacto
        }

        [Theory] // USR-06/07 — teléfono fuera de rango (validator, vía pipeline): <10 o >13 dígitos
        [InlineData(123456789L)]      // 9 dígitos (corto)
        [InlineData(12345678901234L)] // 14 dígitos (largo)
        public async Task Alta_con_telefono_fuera_de_rango_devuelve_400(long telefono)
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                userName = "tel-malo", nombre = "N", apellido = "A", email = "tel-malo@tech-bi.com",
                telefono,
                roles = Array.Empty<object>(), usuarioAplicaciones = Array.Empty<object>(),
            });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorValidation);
        }

        [Fact] // USR-58/59 — mi-perfil devuelve el perfil del usuario del contexto (no recibe id)
        public async Task MiPerfil_devuelve_el_perfil_del_contexto()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.GetAsync("/api/general/usuarios/mi-perfil");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var perfil = await resp.Content.ReadFromJsonAsync<Perfil>();
            Assert.Equal("userb", perfil!.UserName); // siempre el del contexto
        }

        private sealed record Perfil(string UserName);
    }
}
