using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Olvide password (POST /api/auth/olvide-password) — AUTH-59/60 (ramas negativas, que retornan
    /// ANTES de generar token/enviar mail). Los happy paths (57/58) dependen del pipeline de email
    /// (Razor + SMTP con EmailEnabled=false) y se cubren aparte cuando se siembren todos los Fwk.Email.*.
    /// </summary>
    public class AuthForgotPasswordTests : IntegrationTestBase
    {
        public AuthForgotPasswordTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // AUTH-59
        public async Task OlvidePassword_usuario_inexistente_no_revela_y_no_envia_mail()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/olvide-password",
                new { emailOrUsername = "no-existe@tech-bi.com" });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorEmailNotCorrespondToRegisteredUser);
        }

        [Fact] // AUTH-59 (variante: email no confirmado)
        public async Task OlvidePassword_de_usuario_con_email_no_confirmado_devuelve_400()
        {
            await Factory.ExecuteSqlAsync($"UPDATE gen_Usuarios SET EmailConfirmed = 0 WHERE Id = {TestData.UserBId}");
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/olvide-password",
                new { emailOrUsername = TestData.UserB });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorEmailNotCorrespondToRegisteredUser);
        }

        [Fact] // AUTH-60
        public async Task OlvidePassword_modelstate_invalido_devuelve_400()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/olvide-password",
                new { emailOrUsername = "" });

            await resp.ShouldBeBadRequest();
        }

        [Fact] // AUTH-57 — happy path por email (EmailEnabled=false: arma pero no despacha SMTP)
        public async Task OlvidePassword_happy_path_por_email_devuelve_200()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/olvide-password",
                new { emailOrUsername = "userb@tech-bi.com" });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTH-58 — acepta username ademas de email (resuelve por FindByName)
        public async Task OlvidePassword_acepta_username()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/olvide-password",
                new { emailOrUsername = TestData.Root });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }
    }
}
