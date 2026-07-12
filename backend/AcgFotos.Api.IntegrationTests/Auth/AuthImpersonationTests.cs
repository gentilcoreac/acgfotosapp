using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// Impersonalizacion (ADR-0002) — AUTH-67..83. root opera "como" un usuario destino: token scopeado
    /// (isRoot=false) + claim firmado impersonatedBy + cookie-overlay firmada. Solo root (o root ya
    /// impersonando) puede; el destino se valida contra DB ignorando el filtro multi-tenant.
    /// </summary>
    public class AuthImpersonationTests : IntegrationTestBase
    {
        public AuthImpersonationTests(TestWebApplicationFactory factory) : base(factory) { }

        private const string ImpersonationCookie = "impersonation";

        private static Dictionary<string, string> ClaimsOf(string jwt) =>
            new JwtSecurityTokenHandler().ReadJwtToken(jwt).Claims.ToDictionary(c => c.Type, c => c.Value);

        /// <summary>root impersona a userb (t2) y devuelve la respuesta cruda.</summary>
        private async Task<HttpResponseMessage> StartAsRootAsync(long tenantId = TestData.ActiveTenantId, long userId = TestData.UserBId)
        {
            using var root = await CreateAuthenticatedClientAsync(); // bearer de root
            return await root.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId, userId });
        }

        [Fact] // AUTH-67
        public async Task Start_happy_path_emite_token_scopeado_al_destino_con_impersonatedBy()
        {
            var resp = await StartAsRootAsync();

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            var claims = ClaimsOf(dto!.Token);
            Assert.Equal("userb", claims[JwtRegisteredClaimNames.Sub]);   // identidad del destino
            Assert.Equal("2", claims["tenant"]);
            Assert.Equal("False", claims["isRoot"]);
            Assert.Equal("1", claims["impersonatedBy"]);                  // root real preservado
            Assert.NotNull(ExtractCookieValue(resp, ImpersonationCookie)); // overlay firmada
        }

        [Fact] // AUTH-68
        public async Task Start_sin_bearer_devuelve_401()
        {
            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId = 2, userId = 10 });
            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-69
        public async Task Start_por_usuario_no_root_devuelve_403()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var resp = await client.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId = 2, userId = 10 });

            await resp.ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorDataInvalid);
        }

        [Fact] // AUTH-70 — revalida que el root real siga siendo root contra DB
        public async Task Start_revalida_root_contra_DB()
        {
            using var root = await CreateAuthenticatedClientAsync(); // token con isRoot=true (root en t1)
            // El root pierde el rol root (se mueve de tenant) DESPUES de emitir el token.
            await Factory.ExecuteSqlAsync($"UPDATE gen_Usuarios SET TenantId = {TestData.ActiveTenantId} WHERE Id = {TestData.RootId}");

            var resp = await root.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId = 2, userId = 10 });

            await resp.ShouldBeStatus(HttpStatusCode.Forbidden);
        }

        [Fact] // AUTH-71
        public async Task Start_target_inexistente_o_tenant_no_coincide_devuelve_400()
        {
            using var root = await CreateAuthenticatedClientAsync();

            // userb existe pero esta en tenant 2, no en 1 -> mismatch.
            var resp = await root.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId = 1, userId = 10 });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotFound);
        }

        [Fact] // AUTH-72
        public async Task Start_hacia_tenant_inactivo_devuelve_400()
        {
            using var root = await CreateAuthenticatedClientAsync();

            // adminb (11) esta en el tenant 3 (inactivo); tenant coincide pero esta inactivo.
            var resp = await root.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId = 3, userId = 11 });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantNotActive);
        }

        [Fact] // AUTH-73 — la licencia del destino NO bloquea la impersonalizacion
        public async Task Start_no_bloquea_por_licencia_del_destino()
        {
            await Factory.ExecuteSqlAsync($"DELETE FROM gen_UsuarioTipoLicencia WHERE UsuarioId = {TestData.UserBId}");

            var resp = await StartAsRootAsync(); // userb sin licencia, tenant 2 activo
            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTH-74
        public async Task Start_modelstate_invalido_devuelve_400()
        {
            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/auth/impersonation/start", new { tenantId = 0, userId = 0 });
            await resp.ShouldBeBadRequest();
        }

        [Fact] // AUTH-76
        public async Task Stop_happy_path_vuelve_a_root_y_borra_overlay()
        {
            // 1) Obtener un token de impersonalizacion (impersonatedBy=1).
            var start = await StartAsRootAsync();
            var impToken = (await start.Content.ReadFromJsonAsync<TokenModelDto>())!.Token;

            // 2) Stop usando ese token como bearer.
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", impToken);
            var resp = await client.PostAsync("/api/auth/impersonation/stop", null);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<TokenModelDto>();
            var claims = ClaimsOf(dto!.Token);
            Assert.Equal("root", claims[JwtRegisteredClaimNames.Sub]); // de vuelta como root
            Assert.DoesNotContain("impersonatedBy", claims.Keys);
            Assert.Equal(string.Empty, ExtractCookieValue(resp, ImpersonationCookie)); // overlay borrada
        }

        [Fact] // AUTH-77
        public async Task Stop_sin_impersonalizacion_activa_devuelve_400()
        {
            using var root = await CreateAuthenticatedClientAsync(); // root normal, sin impersonatedBy
            var resp = await root.PostAsync("/api/auth/impersonation/stop", null);

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorDataInvalid);
        }

        [Fact] // AUTH-79
        public async Task Stop_sin_bearer_devuelve_401()
        {
            using var client = CreateClient();
            var resp = await client.PostAsync("/api/auth/impersonation/stop", null);
            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-80
        public async Task ImpersonatableUsers_lista_los_usuarios_del_tenant()
        {
            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.GetAsync("/api/auth/impersonation/users/2");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var users = await resp.Content.ReadFromJsonAsync<List<ImpersonatableDto>>();
            Assert.Contains(users!, u => u.UserName == "userb");
        }

        [Fact] // AUTH-81
        public async Task ImpersonatableUsers_por_no_root_devuelve_403()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var resp = await client.GetAsync("/api/auth/impersonation/users/2");
            await resp.ShouldBeStatus(HttpStatusCode.Forbidden);
        }

        [Fact] // AUTH-82
        public async Task ImpersonatableUsers_sin_bearer_devuelve_401()
        {
            using var client = CreateClient();
            var resp = await client.GetAsync("/api/auth/impersonation/users/2");
            await resp.ShouldBeStatus(HttpStatusCode.Unauthorized);
        }

        [Fact] // AUTH-83 — el DTO no expone PasswordHash/SecurityStamp
        public async Task ImpersonatableUsers_no_expone_campos_sensibles()
        {
            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.GetAsync("/api/auth/impersonation/users/2");
            var json = await resp.Content.ReadAsStringAsync();

            Assert.DoesNotContain("passwordhash", json.ToLowerInvariant());
            Assert.DoesNotContain("securitystamp", json.ToLowerInvariant());
        }

        [Fact] // AUTH-75 — re-impersonar: ya impersonando, cambia de target (usa ImpersonatedBy como root real)
        public async Task Re_impersonar_cambia_de_target()
        {
            var start = await StartAsRootAsync(); // root -> userb
            var impToken = (await start.Content.ReadFromJsonAsync<TokenModelDto>())!.Token;

            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", impToken);
            var resp = await client.PostAsJsonAsync("/api/auth/impersonation/start",
                new { tenantId = TestData.ActiveTenantId, userId = TestData.UserB2Id });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var claims = ClaimsOf((await resp.Content.ReadFromJsonAsync<TokenModelDto>())!.Token);
            Assert.Equal("userb2", claims[JwtRegisteredClaimNames.Sub]);
            Assert.Equal("1", claims["impersonatedBy"]); // sigue siendo root (1) el real
        }

        [Fact] // AUTH-78 — stop con el root real ya borrado: 400 ErrorUserNotFound (cookie igual se limpia)
        public async Task Stop_con_root_real_borrado_devuelve_400()
        {
            var start = await StartAsRootAsync();
            var impToken = (await start.Content.ReadFromJsonAsync<TokenModelDto>())!.Token;

            await Factory.ExecuteSqlAsync(
                $"DELETE FROM gen_RefreshTokens WHERE UserId = {TestData.RootId}; DELETE FROM gen_Usuarios WHERE Id = {TestData.RootId};");

            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", impToken);
            var resp = await client.PostAsync("/api/auth/impersonation/stop", null);

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotFound);
        }

        private sealed class ImpersonatableDto
        {
            public long Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Apellido { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
