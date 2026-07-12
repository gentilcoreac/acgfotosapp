using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Tipos de licencia (190) — endpoint dedicado POST /tipos-licencia/{id}/set-default-tenant para el
    /// flag admin-only EsDefaultParaNuevoTenant (mismo patrón que Roles 160: guarda IsRoot server-side en
    /// el AppService). Seed: tipoLic 1 (EsDefault=0), tipoLic 2 (EsDefault=1). Actor root vs userb (no-root).
    /// </summary>
    public class TipoLicenciaSetDefaultTenantTests : IntegrationTestBase
    {
        public TipoLicenciaSetDefaultTenantTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task<int> FlagAsync(long id) =>
            Factory.QueryScalarAsync<int>($"SELECT CAST(EsDefaultParaNuevoTenant AS int) FROM gen_TipoLicencia WHERE Id = {id}");

        [Fact] // TLI-42 — root marca el flag en true (tipoLic 1, arranca false)
        public async Task Root_marca_flag_true()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/1/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });
            await resp.ShouldBeOk();

            Assert.Equal(1, await FlagAsync(1));
        }

        [Fact] // TLI-43 — root apaga el flag (tipoLic 2, arranca true)
        public async Task Root_apaga_flag_false()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/2/set-default-tenant",
                new { esDefaultParaNuevoTenant = false });
            await resp.ShouldBeOk();

            Assert.Equal(0, await FlagAsync(2));
        }

        [Fact] // TLI-44 — no-root: rechazo server-side (BusinessValidationException), el flag NO cambia
        public async Task No_root_rechazado_server_side()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root, tenant 2
            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/1/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            Assert.Equal(0, await FlagAsync(1)); // intacto
        }

        [Fact] // TLI-45 — root, id inexistente: entidad null => BusinessValidationException, no 500 crudo
        public async Task Id_inexistente_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/999999/set-default-tenant",
                new { esDefaultParaNuevoTenant = true });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorGenericInternal);
        }
    }
}
