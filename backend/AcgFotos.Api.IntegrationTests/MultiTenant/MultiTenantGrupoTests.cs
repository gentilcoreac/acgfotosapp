using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.MultiTenant
{
    /// <summary>
    /// MT-05/06/07 sobre Grupo: a diferencia de Usuario (alta propia que bypassa el guard), Grupo usa el
    /// pipeline base (ExtendedEntityAppServiceBase.ExecuteCreate -> SetAppContextTenant), asi que se ve el
    /// guard AllowSaveMultiTenantEntities: root en su propio contexto NO persiste; impersonando SI; y las
    /// colecciones hijas heredan el TenantId del padre.
    /// </summary>
    public class MultiTenantGrupoTests : IntegrationTestBase
    {
        public MultiTenantGrupoTests(TestWebApplicationFactory factory) : base(factory) { }

        private static object GrupoBody(string nombre, long[]? rolIds = null) => Payloads.Grupo(nombre, rolIds: rolIds);

        [Fact] // MT-06 — root en su propio contexto NO puede persistir una entidad multi-tenant
        public async Task Root_en_su_contexto_no_puede_crear_grupo()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root, tenant raiz, sin impersonar

            var resp = await client.PostAsJsonAsync("/api/general/grupos/update", GrupoBody("g-root"));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantNotRoot); // AllowSaveMultiTenantEntities=false
            var count = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Grupos WHERE Nombre = 'g-root'");
            Assert.Equal(0, count);
        }

        [Fact] // MT-07 — root IMPERSONANDO sí persiste en el tenant destino
        public async Task Root_impersonando_puede_crear_grupo_en_el_destino()
        {
            string impToken;
            using (var root = await CreateAuthenticatedClientAsync())
            {
                var start = await root.PostAsJsonAsync("/api/auth/impersonation/start",
                    new { tenantId = TestData.ActiveTenantId, userId = TestData.UserBId });
                impToken = (await start.Content.ReadFromJsonAsync<TokenModelDto>())!.Token;
            }

            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", impToken);
            var resp = await client.PostAsJsonAsync("/api/general/grupos/update", GrupoBody("g-imp"));

            await resp.ShouldBeOk();
            var tenantId = await Factory.QueryScalarAsync<long>("SELECT TenantId FROM gen_Grupos WHERE Nombre = 'g-imp'");
            Assert.Equal(TestData.ActiveTenantId, tenantId);
        }

        [Fact] // MT-05 — las colecciones hijas heredan el TenantId del padre (SetTenantToChildItems)
        public async Task Colecciones_hijas_heredan_el_tenant_del_padre()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.PostAsJsonAsync("/api/general/grupos/update", GrupoBody("g-mt5", rolIds: new long[] { 1 }));

            await resp.ShouldBeOk();
            var childTenant = await Factory.QueryScalarAsync<long>(
                "SELECT TenantId FROM gen_GrupoRoles WHERE GrupoId = (SELECT Id FROM gen_Grupos WHERE Nombre = 'g-mt5') LIMIT 1");
            Assert.Equal(TestData.ActiveTenantId, childTenant); // el GrupoRol hijo quedo en el tenant 2
        }
    }
}
