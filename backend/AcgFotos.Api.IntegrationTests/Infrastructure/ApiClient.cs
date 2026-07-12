using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Helpers HTTP compartidos por TODAS las bases de test (host con authz off y host con authz on),
    /// para no duplicar el login/bearer/lectura de error/cookies. Las bases exponen wrappers de instancia
    /// finos que delegan acá → una sola fuente de verdad.
    /// </summary>
    public static class ApiClient
    {
        private static readonly WebApplicationFactoryClientOptions NoRedirect = new() { AllowAutoRedirect = false };

        // Cultura fija para los tests: la API resuelve los mensajes (MessagesAPI) por el header
        // "cultureInfo" (AppContext.SetContext); sin header cae al .resx NEUTRO, que es español.
        // Las aserciones de la suite están escritas en inglés (en-US), así que fijamos la cultura
        // acá —un solo lugar— para desacoplar los asserts del copy por defecto del producto (es-AR).
        public static HttpClient Create(WebApplicationFactory<Program> factory)
        {
            var client = factory.CreateClient(NoRedirect);
            client.DefaultRequestHeaders.Add("cultureInfo", "en-US");
            return client;
        }

        /// <summary>Login y devuelve el JWT (Token) crudo. Falla el test si no es 200.</summary>
        public static async Task<string> LoginAsync(
            WebApplicationFactory<Program> factory, string userName = TestData.Root, string password = TestData.Password)
        {
            using var client = Create(factory);
            var resp = await client.PostAsJsonAsync("/api/auth/token", new { userName, password });
            if (!resp.IsSuccessStatusCode)
            {
                // El body en Development trae la excepción real del server: sin esto un 500 es indescifrable.
                var body = await resp.Content.ReadAsStringAsync();
                Assert.Fail($"Login de '{userName}' esperaba 200 pero fue {(int)resp.StatusCode}. Body: {body[..Math.Min(body.Length, 800)]}");
            }
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            Assert.False(string.IsNullOrEmpty(dto!.Token));
            return dto.Token;
        }

        /// <summary>Cliente con Authorization: Bearer ya seteado (sesion real, via /token).</summary>
        public static async Task<HttpClient> AuthedAsync(
            WebApplicationFactory<Program> factory, string userName = TestData.Root, string password = TestData.Password)
        {
            var token = await LoginAsync(factory, userName, password);
            var client = Create(factory);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>Token de impersonalizacion (root → tenant/usuario destino). Scopeado al destino.</summary>
        public static async Task<string> ImpersonationTokenAsync(
            WebApplicationFactory<Program> factory, long tenantId, long userId)
        {
            using var root = await AuthedAsync(factory);
            var resp = await root.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId, userId });
            Assert.True(resp.IsSuccessStatusCode, $"impersonation/start esperaba 200 pero fue {(int)resp.StatusCode}.");
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            return dto!.Token;
        }

        public static async Task<ApiErrorResponse> ReadErrorAsync(HttpResponseMessage resp)
        {
            var body = await resp.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.NotNull(body);
            return body!;
        }

        /// <summary>Valor crudo de una cookie del Set-Cookie (o null si no esta).</summary>
        public static string? CookieValue(HttpResponseMessage resp, string cookieName)
        {
            if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
            var prefix = cookieName + "=";
            foreach (var cookie in cookies)
            {
                if (cookie.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var value = cookie.Substring(prefix.Length);
                    var semicolon = value.IndexOf(';');
                    return semicolon >= 0 ? value.Substring(0, semicolon) : value;
                }
            }
            return null;
        }

        /// <summary>Header Set-Cookie completo de una cookie (para inspeccionar flags).</summary>
        public static string? SetCookieHeader(HttpResponseMessage resp, string cookieName)
        {
            if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
            return cookies.FirstOrDefault(c => c.StartsWith(cookieName + "=", StringComparison.Ordinal));
        }
    }
}
