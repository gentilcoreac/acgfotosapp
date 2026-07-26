using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Refresh (rotacion/replay) y logout — AUTH-22..38. El refresh viaja en cookie HttpOnly; aca se
    /// captura del Set-Cookie del login y se reenvia por header Cookie.
    /// </summary>
    public class AuthRefreshLogoutTests : IntegrationTestBase
    {
        public AuthRefreshLogoutTests(TestWebApplicationFactory factory) : base(factory) { }

        private const string RefreshCookie = "refreshToken";

        /// <summary>Login que devuelve el JWT (bearer) y el refresh crudo de la cookie.</summary>
        private async Task<(string token, string refresh)> LoginCapturingRefreshAsync(string user = TestData.Root)
        {
            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = user, password = TestData.Password });
            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            var refresh = ExtractCookieValue(resp, RefreshCookie);
            Assert.False(string.IsNullOrEmpty(refresh));
            return (dto!.Token, refresh!);
        }

        [Fact] // AUTH-22
        public async Task Refresh_happy_path_rota_el_token_y_revoca_el_anterior()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync();

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var rawB = ExtractCookieValue(resp, RefreshCookie);
            Assert.False(string.IsNullOrEmpty(rawB));
            Assert.NotEqual(rawA, rawB); // cookie rotada

            // El token A queda revocado por rotacion, apuntando a B.
            var hashA = RefreshTokenCrypto.Hash(rawA);
            var hashB = RefreshTokenCrypto.Hash(rawB!);
            var reasonA = await Factory.QueryScalarAsync<string>(
                $"SELECT RevokeReason FROM gen_RefreshTokens WHERE TokenHash = '{hashA}'");
            var replacedBy = await Factory.QueryScalarAsync<string>(
                $"SELECT ReplacedByTokenHash FROM gen_RefreshTokens WHERE TokenHash = '{hashA}'");
            Assert.Equal(RefreshTokenRevokeReasons.Rotated, reasonA);
            Assert.Equal(hashB, replacedBy);
        }

        // OJO catalogo: AUTH-27 (110-auth.md) describe "replay = corta la cadena" SIEMPRE, pero el codigo
        // tiene la ventana de gracia de reuso (ADR-0007 / PR 1503, ver ERR-12/13/14 en 340-errores.md):
        //   - reuso DENTRO de la ventana (default 10s) = benigno (F5 rapido) -> 200, no corta la cadena.
        //   - reuso FUERA de la ventana = replay real -> 401 + revoca toda la cadena (replay_detected).
        // Se cubren las dos ramas (AUTH-27 queda como la rama "fuera de ventana").

        [Fact] // ERR-12/13 — reuso dentro de la ventana de gracia: benigno
        public async Task Refresh_reuso_dentro_de_la_ventana_de_gracia_es_benigno()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync();

            using var c1 = CreateClient();
            c1.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var first = await c1.PostAsync("/api/auth/refresh", null);
            var rawB = ExtractCookieValue(first, RefreshCookie)!;

            // Reuso inmediato de A (dentro de los 10s): debe seguir siendo valido (no replay).
            using var c2 = CreateClient();
            c2.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var reuse = await c2.PostAsync("/api/auth/refresh", null);

            await reuse.ShouldBeStatus(HttpStatusCode.OK);
            // B sigue activo (la cadena NO se corta).
            var hashB = RefreshTokenCrypto.Hash(rawB);
            var reasonB = await Factory.QueryScalarAsync<string>(
                $"SELECT RevokeReason FROM gen_RefreshTokens WHERE TokenHash = '{hashB}'");
            Assert.Null(reasonB);
        }

        [Fact] // AUTH-27 — replay real (fuera de la ventana): corta toda la cadena
        public async Task Refresh_replay_fuera_de_la_ventana_corta_toda_la_cadena()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync();

            using var c1 = CreateClient();
            c1.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var first = await c1.PostAsync("/api/auth/refresh", null);
            var rawB = ExtractCookieValue(first, RefreshCookie)!;

            // Simula que pasó la ventana de gracia: atrasa la revocacion de A 30s.
            var hashA = RefreshTokenCrypto.Hash(rawA);
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_RefreshTokens SET RevokedAt = RevokedAt - interval '30 seconds' WHERE TokenHash = '{hashA}'");

            // Replay de A fuera de ventana: 401 y se revoca la cadena activa (incluido B) por replay.
            using var c2 = CreateClient();
            c2.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var replay = await c2.PostAsync("/api/auth/refresh", null);

            await replay.ShouldBeStatus(HttpStatusCode.Unauthorized);
            var hashB = RefreshTokenCrypto.Hash(rawB);
            var reasonB = await Factory.QueryScalarAsync<string>(
                $"SELECT RevokeReason FROM gen_RefreshTokens WHERE TokenHash = '{hashB}'");
            Assert.Equal(RefreshTokenRevokeReasons.ReplayDetected, reasonB);
        }

        [Fact] // AUTH-23
        public async Task Refresh_sin_cookie_devuelve_401()
        {
            using var client = CreateClient();

            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeError(HttpStatusCode.Unauthorized, MessagesAPI.ErrorCredentialsInvalid);
        }

        [Fact] // AUTH-24
        public async Task Refresh_con_token_forjado_devuelve_401_y_limpia_la_cookie()
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}=token-forjado-que-no-matchea-ningun-hash");

            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
            // ClearRefreshCookie => Set-Cookie con valor vacio.
            Assert.Equal(string.Empty, ExtractCookieValue(resp, RefreshCookie));
        }

        [Fact] // AUTH-36
        public async Task Logout_sin_bearer_devuelve_401()
        {
            using var client = CreateClient();

            var resp = await client.PostAsync("/api/auth/logout", null);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-37
        public async Task Logout_revoca_el_refresh_y_limpia_cookies()
        {
            var (token, rawA) = await LoginCapturingRefreshAsync();

            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");

            var resp = await client.PostAsync("/api/auth/logout", null);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var hashA = RefreshTokenCrypto.Hash(rawA);
            var reasonA = await Factory.QueryScalarAsync<string>(
                $"SELECT RevokeReason FROM gen_RefreshTokens WHERE TokenHash = '{hashA}'");
            Assert.Equal(RefreshTokenRevokeReasons.Logout, reasonA);
            Assert.Equal(string.Empty, ExtractCookieValue(resp, RefreshCookie));
        }

        [Fact] // AUTH-38
        public async Task Logout_sin_cookie_de_refresh_es_idempotente()
        {
            var token = await LoginAsync(); // bearer pero NO reenviamos la cookie

            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await client.PostAsync("/api/auth/logout", null);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTH-25
        public async Task Refresh_con_token_expirado_devuelve_401()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync();
            var hashA = RefreshTokenCrypto.Hash(rawA);
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_RefreshTokens SET ExpiresAt = '2020-01-01' WHERE TokenHash = '{hashA}'");

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-26
        public async Task Refresh_de_token_revocado_por_logout_devuelve_401()
        {
            var (token, rawA) = await LoginCapturingRefreshAsync();

            using (var logoutClient = CreateClient())
            {
                logoutClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                logoutClient.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
                await logoutClient.PostAsync("/api/auth/logout", null);
            }

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-28
        public async Task Refresh_de_usuario_bloqueado_devuelve_401()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync(TestData.UserB);
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Usuarios SET LockoutEnd = now() + interval '1 hour' WHERE Id = {TestData.UserBId}");

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-29
        public async Task Refresh_no_root_con_tenant_inactivo_devuelve_401()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync(TestData.UserB);
            await Factory.ExecuteSqlAsync($"UPDATE gen_Tenants SET Activo = false WHERE Id = {TestData.ActiveTenantId}");

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeError(HttpStatusCode.Unauthorized, MessagesAPI.ErrorTenantNotActive);
        }

        [Fact] // AUTH-30
        public async Task Refresh_no_root_sin_licencia_devuelve_401()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync(TestData.UserB);
            await Factory.ExecuteSqlAsync($"DELETE FROM gen_UsuarioTipoLicencia WHERE UsuarioId = {TestData.UserBId}");

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-35 — refresh es AllowAnonymous: anda solo con cookie, sin bearer.
        public async Task Refresh_no_requiere_bearer()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync();

            using var client = CreateClient(); // sin Authorization header
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTH-90 — el refresh de root resuelve por hash ignorando el filtro multi-tenant
        public async Task Refresh_de_root_no_filtra_por_tenant()
        {
            var (_, rawA) = await LoginCapturingRefreshAsync(TestData.Root); // token persistido con TenantId raiz

            using var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={rawA}");
            var resp = await client.PostAsync("/api/auth/refresh", null);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            Assert.False(string.IsNullOrEmpty(ExtractCookieValue(resp, RefreshCookie))); // rotado, no perdido
        }
    }
}
