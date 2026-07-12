using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Resetear password (POST /api/auth/resetear-password) — AUTH-62..65. El happy path usa un token
    /// de reseteo de Identity REAL. OJO: la rama de "code invalido" (AUTH-64) devuelve un string[] CRUDO
    /// (no ApiErrorResponse) — inconsistencia con ADR-0007 que se asienta como hallazgo.
    /// </summary>
    public class AuthResetPasswordTests : IntegrationTestBase
    {
        public AuthResetPasswordTests(TestWebApplicationFactory factory) : base(factory) { }

        private const string NewValidPassword = "NewPass@2026!";

        [Fact] // AUTH-62
        public async Task ResetearPassword_happy_path_resetea_y_revoca_refresh()
        {
            await LoginAsync(TestData.UserB);                                  // crea un refresh para userb
            var code = await Factory.GeneratePasswordResetTokenAsync(TestData.UserB);
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/resetear-password", new
            {
                emailOrUsername = TestData.UserB,
                code,
                password = NewValidPassword,
                confirmPassword = NewValidPassword,
            });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var revocados = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_RefreshTokens WHERE UserId = {TestData.UserBId} AND RevokeReason = 'password_changed'");
            Assert.True(revocados >= 1);
        }

        [Fact] // AUTH-63
        public async Task ResetearPassword_usuario_inexistente_devuelve_mensaje_generico()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/resetear-password", new
            {
                emailOrUsername = "no-existe",
                code = "x",
                password = NewValidPassword,
                confirmPassword = NewValidPassword,
            });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorDataInvalid); // no revela inexistencia
        }

        [Fact] // AUTH-64 — code invalido: 400 con string[] CRUDO (inconsistencia ADR-0007)
        public async Task ResetearPassword_con_code_invalido_devuelve_400()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/resetear-password", new
            {
                emailOrUsername = TestData.UserB,
                code = "codigo-invalido",
                password = NewValidPassword,
                confirmPassword = NewValidPassword,
            });

            // OJO: status-only (ShouldBeStatus, NO ShouldBeBadRequest) — el body NO es ApiErrorResponse, así
            // que parsearlo reventaría. El controller devuelve result.Errors.Select(Description) -> array JSON.
            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            var errors = await resp.Content.ReadFromJsonAsync<string[]>();
            Assert.NotNull(errors);
            Assert.NotEmpty(errors!);
        }

        [Fact] // AUTH-65
        public async Task ResetearPassword_modelstate_invalido_devuelve_400()
        {
            using var client = CreateClient();

            // Password de 3 chars viola StringLength(min 6) -> ModelState invalido.
            var resp = await client.PostAsJsonAsync("/api/auth/resetear-password", new
            {
                emailOrUsername = TestData.UserB,
                code = "x",
                password = "123",
                confirmPassword = "123",
            });

            await resp.ShouldBeBadRequest();
        }
    }
}
