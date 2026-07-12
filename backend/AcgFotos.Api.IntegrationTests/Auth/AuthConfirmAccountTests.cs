using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Confirmacion de cuenta (POST /api/auth/confirmar-cuenta) — AUTH-51..54. El happy path usa un
    /// token de confirmacion de Identity REAL generado desde el host (mismas keys de data-protection).
    /// </summary>
    public class AuthConfirmAccountTests : IntegrationTestBase
    {
        public AuthConfirmAccountTests(TestWebApplicationFactory factory) : base(factory) { }

        private const string ValidPassword = "NewPass@2026!"; // cumple policy (12+, mayus/minus/digito/especial)

        [Fact] // AUTH-51
        public async Task ConfirmarCuenta_happy_path_confirma_email_y_setea_password()
        {
            var code = await Factory.GenerateEmailConfirmationTokenAsync(TestData.Pending);
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/confirmar-cuenta", new
            {
                userId = TestData.PendingId,
                code,
                password = ValidPassword,
                confirmPassword = ValidPassword,
            });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var confirmado = await Factory.QueryScalarAsync<int>(
                $"SELECT EmailConfirmed FROM gen_Usuarios WHERE Id = {TestData.PendingId}");
            var tienePassword = await Factory.QueryScalarAsync<int>(
                $"SELECT CASE WHEN PasswordHash IS NULL THEN 0 ELSE 1 END FROM gen_Usuarios WHERE Id = {TestData.PendingId}");
            Assert.Equal(1, confirmado);
            Assert.Equal(1, tienePassword);
        }

        [Fact] // AUTH-52
        public async Task ConfirmarCuenta_con_userId_invalido_o_code_null_devuelve_400()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/confirmar-cuenta", new
            {
                userId = 0,
                code = (string?)null,
                password = ValidPassword,
                confirmPassword = ValidPassword,
            });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorRequiredInfoGet);
        }

        [Fact] // AUTH-53
        public async Task ConfirmarCuenta_de_usuario_inexistente_devuelve_400()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/confirmar-cuenta", new
            {
                userId = 999999,
                code = "cualquier-cosa",
                password = ValidPassword,
                confirmPassword = ValidPassword,
            });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotFound);
        }

        [Fact] // AUTH-54
        public async Task ConfirmarCuenta_de_cuenta_ya_activa_devuelve_mensaje_de_ya_confirmada()
        {
            using var client = CreateClient();

            // root ya tiene EmailConfirmed=1 y password.
            var resp = await client.PostAsJsonAsync("/api/auth/confirmar-cuenta", new
            {
                userId = TestData.RootId,
                code = "cualquier-cosa",
                password = ValidPassword,
                confirmPassword = ValidPassword,
            });

            await resp.ShouldBeBadRequest(MessagesAPI.SuccessAccountConfirm2);
        }

        [Fact] // AUTH-55
        public async Task ConfirmarCuenta_con_code_invalido_devuelve_400()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/confirmar-cuenta", new
            {
                userId = TestData.PendingId,
                code = "code-invalido-no-emitido-por-identity",
                password = ValidPassword,
                confirmPassword = ValidPassword,
            });

            await resp.ShouldBeBadRequest();
        }

        [Fact] // AUTH-56
        public async Task ConfirmarCuenta_con_password_corta_devuelve_400()
        {
            var code = await Factory.GenerateEmailConfirmationTokenAsync(TestData.Pending);
            using var client = CreateClient();

            // Code valido -> confirma email; pero la password "123" no cumple la policy de Identity.
            var resp = await client.PostAsJsonAsync("/api/auth/confirmar-cuenta", new
            {
                userId = TestData.PendingId,
                code,
                password = "123",
                confirmPassword = "123",
            });

            await resp.ShouldBeBadRequest();
        }
    }
}
