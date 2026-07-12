using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Cambio de password autenticado — AUTH-46..48. OJO con el catalogo: el modelo real es
    /// { CurrentPassword, NewPassword, NewConfirmPassword } (no "OldPassword/ConfirmPassword"), y
    /// NewPassword NO tiene StringLength: la fortaleza la valida Identity (ChangePasswordAsync), no
    /// ModelState. Solo el Compare (NewConfirmPassword) cae por ModelState.
    /// </summary>
    public class AuthPasswordTests : IntegrationTestBase
    {
        public AuthPasswordTests(TestWebApplicationFactory factory) : base(factory) { }

        // Cumple la policy: 12+ chars, mayus/minus/digito/especial, 4+ unicos.
        private const string NewValidPassword = "NewPass@2026!";

        [Fact] // AUTH-46
        public async Task CambiarPassword_happy_path_revoca_todos_los_refresh()
        {
            using var client = await CreateAuthenticatedClientAsync(); // login de root => emite un refresh

            var resp = await client.PostAsJsonAsync("/api/auth/cambiar-password", new
            {
                currentPassword = TestData.Password,
                newPassword = NewValidPassword,
                newConfirmPassword = NewValidPassword,
            });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var revocados = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_RefreshTokens WHERE UserId = {TestData.RootId} AND RevokeReason = 'password_changed'");
            Assert.True(revocados >= 1, "el cambio de password debe revocar los refresh activos del usuario");
        }

        [Fact] // AUTH-47
        public async Task CambiarPassword_con_actual_incorrecta_devuelve_400_y_no_revoca()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.PostAsJsonAsync("/api/auth/cambiar-password", new
            {
                currentPassword = "actual-incorrecta",
                newPassword = NewValidPassword,
                newConfirmPassword = NewValidPassword,
            });

            await resp.ShouldBeBadRequest();
            var revocados = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_RefreshTokens WHERE UserId = {TestData.RootId} AND RevokeReason = 'password_changed'");
            Assert.Equal(0, revocados); // password mal => no se tocan las sesiones
        }

        [Fact] // AUTH-48 (rama ModelState: confirm no coincide)
        public async Task CambiarPassword_con_confirmacion_distinta_devuelve_400()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.PostAsJsonAsync("/api/auth/cambiar-password", new
            {
                currentPassword = TestData.Password,
                newPassword = NewValidPassword,
                newConfirmPassword = "otra-cosa-distinta",
            });

            var error = await resp.ShouldBeBadRequest();
            Assert.False(string.IsNullOrEmpty(error.Message));
        }

        [Fact] // AUTH-50 — OJO catalogo: con bearer valido y el user del token renombrado/borrado, la
        // revalidacion de securityStamp (OnTokenValidated, por UserName+TenantId) rechaza el token con
        // 401 ANTES del 400 ErrorUserNotFound del controller -> ese branch es inalcanzable con bearer valido.
        public async Task CambiarPassword_con_usuario_del_token_inexistente_es_rechazado_en_auth()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // token con UserName=userb
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Usuarios SET UserName = 'userb-renamed', NormalizedUserName = 'USERB-RENAMED' WHERE Id = {TestData.UserBId}");

            var resp = await client.PostAsJsonAsync("/api/auth/cambiar-password", new
            {
                currentPassword = TestData.Password,
                newPassword = NewValidPassword,
                newConfirmPassword = NewValidPassword,
            });

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }
    }
}
