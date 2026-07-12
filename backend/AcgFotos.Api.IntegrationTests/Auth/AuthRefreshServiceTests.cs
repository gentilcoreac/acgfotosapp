using Microsoft.Extensions.DependencyInjection;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// RefreshTokenService a nivel servicio (AUTH-39/42/43/44/45). Se resuelve el servicio/repo REALES
    /// de un scope del host (repo + DbContext + UnitOfWork contra la base de tests) en vez de mockear:
    /// ejercita la persistencia real (hash, metadatos, rotacion, ignore del filtro de tenant).
    /// </summary>
    public class AuthRefreshServiceTests : IntegrationTestBase
    {
        public AuthRefreshServiceTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task<string> RevokeReasonAsync(string rawToken) =>
            Factory.QueryScalarAsync<string>(
                $"SELECT RevokeReason FROM gen_RefreshTokens WHERE TokenHash = '{RefreshTokenCrypto.Hash(rawToken)}'");

        [Fact] // AUTH-42 — IssueAsync persiste hash + expiracion (14d) y entrega el raw una sola vez
        public async Task IssueAsync_persiste_hash_y_expiracion()
        {
            using var scope = Factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

            var before = DateTime.UtcNow;
            var issued = await service.IssueAsync(TestData.UserBId, TestData.ActiveTenantId);

            var hash = RefreshTokenCrypto.Hash(issued.Token);
            var rows = await Factory.QueryScalarAsync<int>($"SELECT COUNT(*) FROM gen_RefreshTokens WHERE TokenHash = '{hash}'");
            Assert.Equal(1, rows);
            // ExpiresAt = now + 14d (default).
            Assert.True(issued.ExpiresAt > before.AddDays(13) && issued.ExpiresAt < before.AddDays(15));
        }

        [Fact] // AUTH-43 — RotateAsync linkea old->new (RevokeReason=rotated, ReplacedByTokenHash)
        public async Task RotateAsync_linkea_old_con_new()
        {
            using var scope = Factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

            var a = await service.IssueAsync(TestData.UserBId, TestData.ActiveTenantId);
            var b = await service.RotateAsync(a.Token, TestData.UserBId, TestData.ActiveTenantId);

            var hashB = RefreshTokenCrypto.Hash(b.Token);
            var replacedBy = await Factory.QueryScalarAsync<string>(
                $"SELECT ReplacedByTokenHash FROM gen_RefreshTokens WHERE TokenHash = '{RefreshTokenCrypto.Hash(a.Token)}'");
            var bRows = await Factory.QueryScalarAsync<int>($"SELECT COUNT(*) FROM gen_RefreshTokens WHERE TokenHash = '{hashB}'");
            Assert.Equal(RefreshTokenRevokeReasons.Rotated, await RevokeReasonAsync(a.Token));
            Assert.Equal(hashB, replacedBy);
            Assert.Equal(1, bRows);
        }

        [Fact] // AUTH-39 — RevokeAsync no re-revoca un token ya revocado (preserva motivo/fecha original)
        public async Task RevokeAsync_no_re_revoca()
        {
            using var scope = Factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

            var t = await service.IssueAsync(TestData.UserBId, TestData.ActiveTenantId);
            await service.RevokeAsync(t.Token, RefreshTokenRevokeReasons.Logout);
            await service.RevokeAsync(t.Token, RefreshTokenRevokeReasons.AdminRevoke); // segundo intento

            Assert.Equal(RefreshTokenRevokeReasons.Logout, await RevokeReasonAsync(t.Token)); // no cambio
        }

        [Fact] // AUTH-44 — GetByHashAsync ignora el filtro multi-tenant (no pierde el token cross-tenant)
        public async Task GetByHashAsync_ignora_filtro_de_tenant()
        {
            using var scope = Factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            var issued = await service.IssueAsync(TestData.UserBId, TestData.ActiveTenantId); // token de tenant 2

            var found = await repo.GetByHashAsync(RefreshTokenCrypto.Hash(issued.Token));
            Assert.NotNull(found);
            Assert.Equal(TestData.ActiveTenantId, found!.TenantId); // encontrado pese al filtro de tenant
        }

        [Fact] // AUTH-45 — RevokeAllActiveForUserAsync revoca solo los ACTIVOS (no pisa el motivo previo)
        public async Task RevokeAllForUser_solo_revoca_los_activos()
        {
            using var scope = Factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

            var t1 = await service.IssueAsync(TestData.UserBId, TestData.ActiveTenantId);
            var t2 = await service.IssueAsync(TestData.UserBId, TestData.ActiveTenantId);
            await service.RevokeAsync(t1.Token, RefreshTokenRevokeReasons.Logout); // t1 ya revocado (logout)

            await service.RevokeAllForUserAsync(TestData.UserBId, RefreshTokenRevokeReasons.PasswordChanged);

            Assert.Equal(RefreshTokenRevokeReasons.Logout, await RevokeReasonAsync(t1.Token));          // intacto
            Assert.Equal(RefreshTokenRevokeReasons.PasswordChanged, await RevokeReasonAsync(t2.Token)); // revocado ahora
        }
    }
}
