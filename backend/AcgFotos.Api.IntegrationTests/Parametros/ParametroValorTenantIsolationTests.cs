using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Parametros
{
    /// <summary>
    /// Parametros por tenant (210) — BASELINE de aislamiento multi-tenant (el test mas basico del area).
    /// ANTES ParametroValorTenant heredaba de EntityBase con TenantId plano y SIN scoping => cruce de
    /// datos: cualquier tenant leia/escribia/borraba overrides de otro (PVT-12/13/14/15, hallazgo #12).
    /// FIX (Opcion A — gatear cross-tenant a ROOT): la entidad sigue NO siendo IMultiTenantEntityBase
    /// porque root administra todos los tenants desde una pantalla root-only centralizada (sin impersonar,
    /// como aplicaciones-tenant-id); el aislamiento de NO-root se hace EXPLICITO en el AppService
    /// (un no-root solo ve/escribe/borra SU tenant; el TenantId del body se ignora). Estos tests verifican:
    /// (a) NO-root aislado, (b) root mantiene el cross-tenant (pantalla de admin intacta).
    /// Los overrides ajenos se siembran por SQL.
    /// </summary>
    public class ParametroValorTenantIsolationTests : IntegrationTestBase
    {
        public ParametroValorTenantIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        private const long ParamId = 9; // Fwk.Email.Port (seed), default "587"

        private sealed record PvtRow(long Id, long TenantId, long ParametroId, string? Valor);
        private sealed record Paginated(IReadOnlyList<PvtRow> Items, int TotalCount);

        /// <summary>Siembra un override por SQL (bypassa la API) y devuelve su Id.</summary>
        private async Task<long> SeedOverrideAsync(long tenantId, string valor, long parametroId = ParamId)
        {
            await Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_ParametrosValoresTenants (Valor, TenantId, ParametroId) VALUES ('{valor}', {tenantId}, {parametroId})");
            return await Factory.QueryScalarAsync<long>(
                $"SELECT Id FROM gen_ParametrosValoresTenants WHERE TenantId = {tenantId} AND ParametroId = {parametroId}");
        }

        [Fact] // PVT-13 (asegurado) — el listado solo trae overrides del tenant del contexto
        public async Task Listado_solo_overrides_del_propio_tenant()
        {
            await SeedOverrideAsync(TestData.ActiveTenantId, "t2-val");   // tenant 2
            await SeedOverrideAsync(TestData.InactiveTenantId, "t3-val"); // tenant 3

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.GetAsync("/api/general/parametros-valores?page=0&pageSize=50");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var page = await resp.Content.ReadFromJsonAsync<Paginated>();
            Assert.All(page!.Items, i => Assert.Equal(TestData.ActiveTenantId, i.TenantId)); // todos del tenant 2
            Assert.Contains(page.Items, i => i.Valor == "t2-val");
            Assert.DoesNotContain(page.Items, i => i.Valor == "t3-val"); // nada del tenant 3
        }

        [Fact] // PVT-14 (asegurado) — no se puede leer por id un override de otro tenant
        public async Task Get_por_id_de_otro_tenant_da_404()
        {
            var ajenoId = await SeedOverrideAsync(TestData.InactiveTenantId, "secreto-t3"); // tenant 3

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.GetAsync($"/api/general/parametros-valores/{ajenoId}");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound); // filtrado => GetById null => 404
            Assert.DoesNotContain("secreto-t3", await resp.Content.ReadAsStringAsync());
        }

        [Fact] // PVT-15 (asegurado) — DELETE por id de otro tenant NO borra el override ajeno
        public async Task Delete_por_id_de_otro_tenant_no_borra()
        {
            var ajenoId = await SeedOverrideAsync(TestData.InactiveTenantId, "t3-val"); // tenant 3

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.DeleteAsync($"/api/general/parametros-valores/{ajenoId}");

            // Idempotente: el endpoint no "ve" la fila ajena (filtrada) -> no-op. La fila SOBREVIVE.
            await resp.ShouldBeOk();
            var sigueExistiendo = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_ParametrosValoresTenants WHERE Id = {ajenoId}");
            Assert.Equal(1, sigueExistiendo);
        }

        [Fact] // PVT-12 (asegurado) — update con tenantId ajeno en el body se persiste bajo el tenant del contexto
        public async Task Update_con_tenantId_ajeno_cae_en_el_propio_tenant()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.PostAsJsonAsync("/api/general/parametros-valores/update",
                new { id = 0, parametroId = ParamId, tenantId = TestData.InactiveTenantId, valor = "x" }); // body dice tenant 3

            await resp.ShouldBeOk();
            var enT2 = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_ParametrosValoresTenants WHERE ParametroId = {ParamId} AND TenantId = {TestData.ActiveTenantId}");
            var enT3 = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_ParametrosValoresTenants WHERE ParametroId = {ParamId} AND TenantId = {TestData.InactiveTenantId}");
            Assert.Equal(1, enT2); // cayo en el tenant del contexto (2)
            Assert.Equal(0, enT3); // el tenantId del body se ignoro
        }

        [Fact] // PVT-03/05 (happy) — valorizar crea el override en el tenant del contexto (tomado server-side)
        public async Task Valorizar_crea_override_en_el_contexto()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.PostAsJsonAsync("/api/general/parametros-valores/valorizar",
                new { nombreParametro = "Fwk.Email.Port", valor = "25" });

            await resp.ShouldBeOk();
            var tenant = await Factory.QueryScalarAsync<long>(
                $"SELECT TenantId FROM gen_ParametrosValoresTenants WHERE ParametroId = {ParamId} AND Valor = '25'");
            Assert.Equal(TestData.ActiveTenantId, tenant);
        }

        [Fact] // PVT-32 (se preserva con Opcion A) — root en su contexto SI puede valorizar (no aplica guard de root)
        public async Task Valorizar_como_root_crea_override_para_su_tenant()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root, contexto propio (sin impersonar)

            var resp = await client.PostAsJsonAsync("/api/general/parametros-valores/valorizar",
                new { nombreParametro = "Fwk.Email.Port", valor = "9" });

            await resp.ShouldBeOk();
            var tenant = await Factory.QueryScalarAsync<long>(
                $"SELECT TenantId FROM gen_ParametrosValoresTenants WHERE ParametroId = {ParamId} AND Valor = '9'");
            Assert.Equal(TestData.RootTenantId, tenant); // override para el tenant raiz (su contexto)
        }

        // --- ROOT cross-tenant: la pantalla root-only centralizada (Opcion A lo PRESERVA) ---

        [Fact] // root administra cualquier tenant: update con tenantId ajeno SI cae en ese tenant (no en el de root)
        public async Task Root_update_con_tenantId_ajeno_cae_en_ese_tenant()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.PostAsJsonAsync("/api/general/parametros-valores/update",
                new { id = 0, parametroId = ParamId, tenantId = TestData.ActiveTenantId, valor = "root-set" }); // body: tenant 2

            await resp.ShouldBeOk();
            var tenant = await Factory.QueryScalarAsync<long>(
                $"SELECT TenantId FROM gen_ParametrosValoresTenants WHERE ParametroId = {ParamId} AND Valor = 'root-set'");
            Assert.Equal(TestData.ActiveTenantId, tenant); // root SI escribe en el tenant que eligio (2)
        }

        [Fact] // root ve/lee overrides de cualquier tenant (lista sin acotar + get por id de otro tenant)
        public async Task Root_ve_overrides_de_otros_tenants()
        {
            var ajenoId = await SeedOverrideAsync(TestData.ActiveTenantId, "t2-val");   // tenant 2
            await SeedOverrideAsync(TestData.InactiveTenantId, "t3-val", parametroId: 10); // tenant 3, otro param

            using var client = await CreateAuthenticatedClientAsync(); // root

            // get por id de un override ajeno -> 200 (root lo ve)
            var get = await client.GetAsync($"/api/general/parametros-valores/{ajenoId}");
            await get.ShouldBeStatus(HttpStatusCode.OK);

            // listado: root ve overrides de >1 tenant
            var list = await client.GetAsync("/api/general/parametros-valores?page=0&pageSize=50");
            var page = await list.Content.ReadFromJsonAsync<Paginated>();
            var tenants = page!.Items.Select(i => i.TenantId).Distinct().ToList();
            Assert.Contains(TestData.ActiveTenantId, tenants);
            Assert.Contains(TestData.InactiveTenantId, tenants);
        }

        [Fact] // root puede borrar el override de otro tenant (reset centralizado)
        public async Task Root_borra_override_de_otro_tenant()
        {
            var ajenoId = await SeedOverrideAsync(TestData.ActiveTenantId, "t2-val"); // tenant 2

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.DeleteAsync($"/api/general/parametros-valores/{ajenoId}");

            await resp.ShouldBeOk();
            var sigue = await Factory.QueryScalarAsync<int>(
                $"SELECT COUNT(*) FROM gen_ParametrosValoresTenants WHERE Id = {ajenoId}");
            Assert.Equal(0, sigue); // root SI borro el override ajeno
        }
    }
}
