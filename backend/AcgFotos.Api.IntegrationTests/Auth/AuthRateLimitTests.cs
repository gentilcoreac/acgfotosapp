using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Rate limiting de auth (AUTH-20/49/61/66). El limiter esta APAGADO en el host base; cada test
    /// arma un host efimero con limites BAJOS (estado del limiter fresco, sin arrastre entre tests) y
    /// excede la ventana para forzar el 429. Particion por IP (compartida en TestServer).
    /// </summary>
    public class AuthRateLimitTests : IntegrationTestBase
    {
        public AuthRateLimitTests(TestWebApplicationFactory factory) : base(factory) { }

        private WebApplicationFactory<Program> RateLimited(int login = 100000, int password = 100000) =>
            Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:Login:PermitLimit"] = login.ToString(),
                    ["RateLimiting:Password:PermitLimit"] = password.ToString(),
                    ["RateLimiting:Global:PermitLimit"] = "1000000", // que no interfiera
                })));

        private static async Task<HttpStatusCode> RepeatAsync(HttpClient client, int times, Func<HttpClient, Task<HttpResponseMessage>> call)
        {
            HttpResponseMessage? last = null;
            for (var i = 0; i < times; i++)
            {
                last = await call(client);
            }
            return last!.StatusCode;
        }

        [Fact] // AUTH-20 — /token excede LoginPolicy -> 429
        public async Task Login_excede_el_rate_limit()
        {
            using var factory = RateLimited(login: 2);
            using var client = factory.CreateClient();

            var status = await RepeatAsync(client, 3, c =>
                c.PostAsJsonAsync("/api/auth/token", new { userName = "nadie", password = "x" }));

            Assert.Equal(HttpStatusCode.TooManyRequests, status);
        }

        [Fact] // AUTH-61 — /olvide-password excede PasswordPolicy -> 429
        public async Task OlvidePassword_excede_el_rate_limit()
        {
            using var factory = RateLimited(password: 2);
            using var client = factory.CreateClient();

            var status = await RepeatAsync(client, 3, c =>
                c.PostAsJsonAsync("/api/auth/olvide-password", new { emailOrUsername = "x@x.com" }));

            Assert.Equal(HttpStatusCode.TooManyRequests, status);
        }

        [Fact] // AUTH-66 — /resetear-password excede PasswordPolicy -> 429
        public async Task ResetearPassword_excede_el_rate_limit()
        {
            using var factory = RateLimited(password: 2);
            using var client = factory.CreateClient();

            var status = await RepeatAsync(client, 3, c =>
                c.PostAsJsonAsync("/api/auth/resetear-password",
                    new { emailOrUsername = "x", code = "y", password = "NewPass@2026!", confirmPassword = "NewPass@2026!" }));

            Assert.Equal(HttpStatusCode.TooManyRequests, status);
        }

        [Fact] // AUTH-49 — /cambiar-password excede PasswordPolicy -> 429 (el login va por LoginPolicy, alto)
        public async Task CambiarPassword_excede_el_rate_limit()
        {
            using var factory = RateLimited(password: 2);

            string token;
            using (var loginClient = factory.CreateClient())
            {
                var login = await loginClient.PostAsJsonAsync("/api/auth/token",
                    new { userName = TestData.UserB, password = TestData.Password });
                token = (await login.Content.ReadFromJsonAsync<TokenModelDto>())!.Token;
            }

            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var status = await RepeatAsync(client, 3, c =>
                c.PostAsJsonAsync("/api/auth/cambiar-password",
                    new { currentPassword = "wrong", newPassword = "NewPass@2026!", newConfirmPassword = "NewPass@2026!" }));

            Assert.Equal(HttpStatusCode.TooManyRequests, status);
        }
    }
}
