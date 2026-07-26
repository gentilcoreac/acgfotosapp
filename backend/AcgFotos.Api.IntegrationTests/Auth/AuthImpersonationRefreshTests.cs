using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Refresh con overlay de impersonalizacion (ADR-0002) — AUTH-31..34. La impersonalizacion se
    /// sostiene a traves de los refresh por la cookie-overlay firmada: refresh la lee para re-emitir el
    /// token scopeado al destino; si ya no es valida, cae a root y limpia la overlay.
    /// </summary>
    public class AuthImpersonationRefreshTests : IntegrationTestBase
    {
        public AuthImpersonationRefreshTests(TestWebApplicationFactory factory) : base(factory) { }

        private const string RefreshCookie = "refreshToken";
        private const string OverlayCookie = "impersonation";

        private static Dictionary<string, string> ClaimsOf(string jwt) =>
            new JwtSecurityTokenHandler().ReadJwtToken(jwt).Claims.ToDictionary(c => c.Type, c => c.Value);

        /// <summary>root loguea (captura refresh) e impersona al target (captura la overlay).</summary>
        private async Task<(string refresh, string overlay)> RootWithOverlayAsync(long tenantId, long userId)
        {
            string refresh, bearer;
            using (var c = CreateClient())
            {
                var login = await c.PostAsJsonAsync("/api/auth/token", new { userName = TestData.Root, password = TestData.Password });
                bearer = (await login.Content.ReadFromJsonAsync<AcgFotos.Core.Security.TokenModelDto>())!.Token;
                refresh = ExtractCookieValue(login, RefreshCookie)!;
            }

            using (var c = CreateClient())
            {
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                var start = await c.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId, userId });
                await start.ShouldBeStatus(HttpStatusCode.OK);
                var overlay = ExtractCookieValue(start, OverlayCookie)!;
                Assert.False(string.IsNullOrEmpty(overlay));
                return (refresh, overlay);
            }
        }

        private async Task<HttpResponseMessage> RefreshWithAsync(string refresh, string overlay)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", $"{RefreshCookie}={refresh}; {OverlayCookie}={overlay}");
            var resp = await client.PostAsync("/api/auth/refresh", null);
            client.Dispose();
            return resp;
        }

        [Fact] // AUTH-31 — overlay vigente: refresh re-emite el token scopeado al destino
        public async Task Refresh_con_overlay_vigente_reemite_token_scopeado()
        {
            var (refresh, overlay) = await RootWithOverlayAsync(TestData.ActiveTenantId, TestData.UserBId);

            var resp = await RefreshWithAsync(refresh, overlay);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var claims = ClaimsOf((await resp.Content.ReadFromJsonAsync<AcgFotos.Core.Security.TokenModelDto>())!.Token);
            Assert.Equal("userb", claims[JwtRegisteredClaimNames.Sub]);
            Assert.Equal("False", claims["isRoot"]);
            Assert.Equal("1", claims["impersonatedBy"]);
            // el refresh se rota igual (anclado a root): hay cookie nueva
            Assert.False(string.IsNullOrEmpty(ExtractCookieValue(resp, RefreshCookie)));
        }

        [Fact] // AUTH-32 — overlay invalida (target borrado): cae a root y limpia la overlay
        public async Task Refresh_con_overlay_invalida_cae_a_root()
        {
            var (refresh, overlay) = await RootWithOverlayAsync(TestData.ActiveTenantId, TestData.UserBId);
            await Factory.ExecuteSqlAsync(
                $"DELETE FROM gen_UsuarioTipoLicencia WHERE UsuarioId = {TestData.UserBId}; DELETE FROM gen_Usuarios WHERE Id = {TestData.UserBId};");

            var resp = await RefreshWithAsync(refresh, overlay);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var claims = ClaimsOf((await resp.Content.ReadFromJsonAsync<AcgFotos.Core.Security.TokenModelDto>())!.Token);
            Assert.Equal("root", claims[JwtRegisteredClaimNames.Sub]);
            Assert.DoesNotContain("impersonatedBy", claims.Keys);
            Assert.Equal(string.Empty, ExtractCookieValue(resp, OverlayCookie)); // overlay limpiada
        }

        [Fact] // AUTH-33 — overlay hacia tenant ahora inactivo: cae a root
        public async Task Refresh_con_overlay_hacia_tenant_inactivo_cae_a_root()
        {
            var (refresh, overlay) = await RootWithOverlayAsync(TestData.ActiveTenantId, TestData.UserBId);
            await Factory.ExecuteSqlAsync($"UPDATE gen_Tenants SET Activo = false WHERE Id = {TestData.ActiveTenantId}");

            var resp = await RefreshWithAsync(refresh, overlay);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var claims = ClaimsOf((await resp.Content.ReadFromJsonAsync<AcgFotos.Core.Security.TokenModelDto>())!.Token);
            Assert.Equal("root", claims[JwtRegisteredClaimNames.Sub]);
            Assert.DoesNotContain("impersonatedBy", claims.Keys);
        }

        [Fact] // AUTH-34 — overlay firmada para OTRO By es ignorada (no escala a otro root)
        public async Task Refresh_ignora_overlay_firmada_para_otro_By()
        {
            // Overlay valida y bien firmada pero con By=99 (otro root); el usuario del refresh es root (1).
            var key = Factory.Services.GetRequiredService<IConfiguration>().GetValue<string>("JwtSecurityToken:key");
            var overlay = ImpersonationCookie.Protect(
                new ImpersonationTicket(TestData.ActiveTenantId, TestData.UserBId, 99, DateTimeOffset.UtcNow.AddMinutes(30)),
                key!);

            string refresh;
            using (var c = CreateClient())
            {
                var login = await c.PostAsJsonAsync("/api/auth/token", new { userName = TestData.Root, password = TestData.Password });
                refresh = ExtractCookieValue(login, RefreshCookie)!;
            }

            var resp = await RefreshWithAsync(refresh, overlay);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var claims = ClaimsOf((await resp.Content.ReadFromJsonAsync<AcgFotos.Core.Security.TokenModelDto>())!.Token);
            Assert.Equal("root", claims[JwtRegisteredClaimNames.Sub]); // overlay de otro By ignorada
            Assert.DoesNotContain("impersonatedBy", claims.Keys);
        }
    }
}
