using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — TEN-18 + FIX #20: al editar un tenant a Activo=false, la edición invoca
    /// <c>UpdateSecurityStampToTenantUsersAsync(tenantId)</c> que bumpea el security stamp de los NO-admin
    /// del tenant DESACTIVADO → sus JWT vigentes fallan en <c>OnTokenValidated</c> (ValidateSecurityStamp) =
    /// corte de sesión inmediato. Antes del fix la query corría bajo el filtro global (baja root-only → scope
    /// al tenant de root, no al desactivado) → no-op cross-tenant. No revienta (AsNoTracking, no repite #16).
    /// </summary>
    public class TenantDeactivationTests : IntegrationTestBase
    {
        public TenantDeactivationTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task<string?> StampAsync(long userId) =>
            Factory.QueryScalarAsync<string?>($"SELECT SecurityStamp FROM gen_Usuarios WHERE Id = {userId}");

        private static object EditarTenant2(bool activo) => new
        {
            id = TestData.ActiveTenantId,
            codigo = "tenant-b",
            nombre = "Tenant B",
            tituloWeb = "Tenant B",
            activo,
            styleSheetCssUrl = "http://x/custom.css", // evita la regeneración de theme (no es el foco)
            tenantLicenses = new[] { new { tipoLicenciaId = 1L, cantidad = 5L, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" } },
        };

        [Fact] // TEN-18 / FIX #20 — desactivar el tenant CORTA la sesión de sus usuarios no-admin: el JWT que
               // userb tenía vigente deja de validar (401), sin tocar a los admins del tenant ni a otros tenants.
        public async Task Desactivar_tenant_corta_sesion_de_sus_usuarios()
        {
            // userb inicia sesión con el tenant 2 ACTIVO y su token funciona.
            using var userbClient = await CreateAuthenticatedClientAsync(TestData.UserB);
            await (await userbClient.GetAsync("/api/general/aplicaciones/aplicaciones-permitidas")).ShouldBeOk();
            using var root = await CreateAuthenticatedClientAsync();

            // Captura DESPUÉS de los logins (el login regenera el propio stamp) → mide solo el efecto del delete.
            var stampAdminB2Antes = await StampAsync(TestData.AdminB2Id); // admin del tenant 2
            var stampRootAntes = await StampAsync(TestData.RootId);       // otro tenant (raíz)

            // root desactiva el tenant 2.
            await (await root.PostAsJsonAsync("/api/general/tenants/update", EditarTenant2(activo: false))).ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT CAST(Activo AS int) FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}"));

            // El JWT que userb seguía usando ahora falla en OnTokenValidated (stamp bumpeado) → 401.
            var resp = await userbClient.GetAsync("/api/general/aplicaciones/aplicaciones-permitidas");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);

            // Granularidad: el admin del tenant 2 NO se corta (puede entrar a un tenant inactivo, AUTH-13) y el
            // tenant raíz no se ve afectado.
            Assert.Equal(stampAdminB2Antes, await StampAsync(TestData.AdminB2Id));
            Assert.Equal(stampRootAntes, await StampAsync(TestData.RootId));

            // El SecurityStamp se genera por fila (Guid.NewGuid() → NEWID() en SQL): dos no-admin del tenant 2
            // (userb y userb2) quedan con stamps DISTINTOS, no un mismo valor compartido.
            Assert.NotEqual(await StampAsync(TestData.UserBId), await StampAsync(TestData.UserB2Id));
        }
    }
}
