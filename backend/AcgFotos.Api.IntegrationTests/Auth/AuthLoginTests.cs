using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Login (POST /api/auth/token) — matriz de AUTH-01..16. Las respuestas de error usan la forma
    /// estandar { message, errors, traceId? } (ApiErrorResponse, ADR-0007); el catalogo todavia las
    /// describe como "string[]" (notacion previa al ADR-0007): se asienta como discrepancia.
    /// </summary>
    public class AuthLoginTests : IntegrationTestBase
    {
        public AuthLoginTests(TestWebApplicationFactory factory) : base(factory) { }

        private const string RefreshCookie = "refreshToken";

        [Fact] // AUTH-01
        public async Task Login_root_happy_path_devuelve_token_y_cookie_de_refresh()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.Root, password = TestData.Password });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            Assert.NotNull(dto);
            Assert.False(string.IsNullOrEmpty(dto!.Token));
            Assert.Equal(TestData.RootTenantId, dto.TenantId);
            Assert.True(dto.HasVerifiedEmail);
            Assert.NotNull(ExtractCookieValue(resp, RefreshCookie));
        }

        [Fact] // AUTH-02 — VERIFICA que el login no-root funcione end-to-end (licencia + tenant activo).
        public async Task Login_no_root_con_licencia_activa_devuelve_token_con_isRoot_false()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = TestData.Password });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            Assert.NotNull(dto);
            Assert.Equal(TestData.ActiveTenantId, dto!.TenantId);

            var claims = new JwtSecurityTokenHandler().ReadJwtToken(dto.Token).Claims
                .ToDictionary(c => c.Type, c => c.Value);
            Assert.Equal("False", claims["isRoot"]);
            Assert.NotNull(ExtractCookieValue(resp, RefreshCookie));
        }

        [Fact] // AUTH-05
        public async Task Login_sin_usuario_ni_password_devuelve_400_con_mensaje_requerido()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token", new { userName = "", password = "" });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserPassRequired);
        }

        [Fact] // AUTH-06
        public async Task Login_usuario_inexistente_devuelve_mensaje_generico()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = "nadie", password = "x" });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorCredentialsInvalid);
        }

        [Fact] // AUTH-08
        public async Task Login_con_email_no_confirmado_devuelve_400()
        {
            await Factory.ExecuteSqlAsync("UPDATE gen_Usuarios SET EmailConfirmed = 0 WHERE UserName = 'userb'");
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = TestData.Password });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorEmailNotValid);
        }

        [Fact] // AUTH-12
        public async Task Login_no_root_con_tenant_inactivo_devuelve_400_antes_de_chequear_password()
        {
            using var client = CreateClient();

            // userc esta en el tenant 3 (inactivo). Hasta con password correcta debe cortar por tenant.
            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserC, password = TestData.Password });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantNotActive);
        }

        [Fact] // AUTH-13 — Administrador ignora el tenant inactivo (pero sigue validando licencia por ser no-root).
        public async Task Login_administrador_ignora_tenant_inactivo()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.AdminB, password = TestData.Password });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTH-16 — root no valida licencia (no tiene ninguna asignada en el seed) y aun asi entra.
        public async Task Login_root_no_valida_licencia()
        {
            var licenciasRoot = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_UsuarioTipoLicencia WHERE UsuarioId = {TestData.RootId}");
            Assert.Equal(0, licenciasRoot); // precondicion: root sin licencia

            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.Root, password = TestData.Password });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTH-07
        public async Task Login_password_incorrecta_reporta_intentos_restantes_e_incrementa_el_contador()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = "password-incorrecta" });

            // ErroresIngresoPermitidos=5; tras el 1er fallo (count=1) quedan 4.
            await resp.ShouldBeBadRequest(string.Format(MessagesAPI.ErrorInputPasswordWrong, 4));

            var failedCount = await Factory.QueryScalarAsync<int>(
                $"SELECT AccessFailedCount FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.Equal(1, failedCount);
        }

        [Fact] // AUTH-09
        public async Task Login_con_cuenta_bloqueada_corta_antes_de_chequear_password()
        {
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Usuarios SET LockoutEnd = DATEADD(hour, 1, SYSDATETIMEOFFSET()) WHERE Id = {TestData.UserBId}");
            using var client = CreateClient();

            // Hasta con la password correcta debe responder "bloqueada".
            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = TestData.Password });

            await resp.ShouldBeBadRequest(string.Format(MessagesAPI.ErrorAccountBlocked, TestData.UserB));
        }

        [Fact] // AUTH-10
        public async Task Login_dispara_lockout_al_alcanzar_el_tope()
        {
            // 4 fallos previos; el 5º (este) alcanza MaxFailedAccessAttempts=5 y bloquea.
            await Factory.ExecuteSqlAsync($"UPDATE gen_Usuarios SET AccessFailedCount = 4 WHERE Id = {TestData.UserBId}");
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = "password-incorrecta" });

            await resp.ShouldBeBadRequest(string.Format(MessagesAPI.ErrorAccountBlocked, TestData.UserB));

            var locked = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_Usuarios WHERE Id = {TestData.UserBId} AND LockoutEnd IS NOT NULL");
            Assert.Equal(1, locked);
        }

        [Fact] // AUTH-11
        public async Task Login_exitoso_resetea_el_contador_de_fallos()
        {
            await Factory.ExecuteSqlAsync($"UPDATE gen_Usuarios SET AccessFailedCount = 3 WHERE Id = {TestData.UserBId}");
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = TestData.Password });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var failedCount = await Factory.QueryScalarAsync<int>(
                $"SELECT AccessFailedCount FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.Equal(0, failedCount);
        }

        [Fact] // AUTH-14
        public async Task Login_no_root_sin_licencia_activa_devuelve_400_sin_cookie()
        {
            await Factory.ExecuteSqlAsync($"DELETE FROM gen_UsuarioTipoLicencia WHERE UsuarioId = {TestData.UserBId}");
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = TestData.Password });

            await resp.ShouldBeBadRequest(string.Format(MessagesAPI.ErrorAccountNotActivelicense, TestData.UserB));
            Assert.Null(ExtractCookieValue(resp, RefreshCookie)); // no se emite refresh sin licencia
        }

        [Fact] // AUTH-15
        public async Task Login_no_root_con_licencia_vencida_devuelve_400_sin_cookie()
        {
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_TenantLicencias SET ExpireDatetime = '2020-01-01' WHERE TenantId = {TestData.ActiveTenantId}");
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.UserB, password = TestData.Password });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorLicenseExpired);
            Assert.Null(ExtractCookieValue(resp, RefreshCookie));
        }

        [Fact] // AUTH-91
        public async Task Cookie_de_refresh_tiene_flags_de_seguridad_correctos()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.Root, password = TestData.Password });

            var setCookie = GetSetCookieHeader(resp, RefreshCookie);
            Assert.NotNull(setCookie);
            var header = setCookie!.ToLowerInvariant();
            Assert.Contains("httponly", header);
            Assert.Contains("secure", header);
            Assert.Contains("samesite=strict", header);
            Assert.Contains("path=/api/auth", header);
        }

        [Fact] // AUTH-19 — el login exitoso registra el historial del usuario (periodo actual)
        public async Task Login_exitoso_registra_historial()
        {
            using var client = CreateClient();

            var resp = await client.PostAsJsonAsync("/api/auth/token",
                new { userName = TestData.Root, password = TestData.Password });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var rows = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_UsuariosHistorial WHERE UsuarioId = {TestData.RootId}");
            Assert.Equal(1, rows);
        }
    }
}
