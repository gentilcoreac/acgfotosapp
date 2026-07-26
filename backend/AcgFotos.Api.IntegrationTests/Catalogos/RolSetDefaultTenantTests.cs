using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Roles (160) — endpoint dedicado POST /roles/{id}/set-default-tenant para el flag admin-only
    /// EsDefaultParaNuevoTenant (separado del Update normal porque RolInputDto NO expone el campo).
    /// La guarda IsRoot es server-side (RolAppService): un no-root recibe BusinessValidationException
    /// aunque el endpoint sea alcanzable (host con authz OFF) — cinturón + tirantes.
    /// Seed: Rol 1 (Administrador, EsDefault=0). Actor root vs userb (no-root, tenant 2).
    /// </summary>
    public class RolSetDefaultTenantTests : IntegrationTestBase
    {
        public RolSetDefaultTenantTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task<int> FlagAsync(long id) =>
            Factory.QueryScalarAsync<int>($"SELECT CAST(EsDefaultParaNuevoTenant AS int) FROM gen_Roles WHERE Id = {id}");

        [Fact] // ROL-31 — root marca el flag en true (persiste)
        public async Task Root_marca_flag_true()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/roles/1/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });
            await resp.ShouldBeOk();

            Assert.Equal(1, await FlagAsync(1));
        }

        [Fact] // ROL-32 — root apaga el flag (vuelve a false)
        public async Task Root_apaga_flag_false()
        {
            await Factory.ExecuteSqlAsync("UPDATE gen_Roles SET EsDefaultParaNuevoTenant = true WHERE Id = 1");

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/roles/1/set-default-tenant",
                new { esDefaultParaNuevoTenant = false });
            await resp.ShouldBeOk();

            Assert.Equal(0, await FlagAsync(1));
        }

        [Fact] // ROL-33 — no-root: rechazo server-side (BusinessValidationException), el flag NO cambia
        public async Task No_root_rechazado_server_side()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root, tenant 2
            var resp = await client.PostAsJsonAsync("/api/general/roles/1/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            Assert.Equal(0, await FlagAsync(1)); // intacto
        }

        [Fact] // ROL-34 — root, id inexistente: rol null => BusinessValidationException, no 500 crudo
        public async Task Id_inexistente_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/roles/999999/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorGenericInternal);
        }
    }
}
