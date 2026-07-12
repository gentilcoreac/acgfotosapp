using System.Net;
using System.Net.Http.Headers;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.LogInfo
{
    /// <summary>
    /// LogInfo (270) — BASELINE de aislamiento, con DOS caminos. LogInfo SI es IMultiTenantEntityBase, asi
    /// que el listado/detalle/csv HEREDADOS los filtra el filtro global de EF (un tenant solo ve sus logs).
    /// El path de SQL CRUDO es `GetForAllTenants` (endpoint AllTenants): ADO.NET que BYPASEA el filtro por
    /// diseno (visor global) y por eso esta gateado a `IsRoot && TenantId == RootTenantId` -> cualquier otro
    /// (no-root, o root en contexto simulado) recibe 400 (BusinessValidationException, NO 401/403). Ese gate
    /// es el punto critico: es la unica via cross-tenant. Filas sembradas por SQL.
    /// </summary>
    public class LogInfoIsolationTests : IntegrationTestBase
    {
        public LogInfoIsolationTests(TestWebApplicationFactory factory) : base(factory) { }

        private Task SeedLogsAsync() => Factory.ExecuteSqlAsync(@"
            INSERT INTO gen_LogInfos (Message, MessageTemplate, Level, TimeStamp, Exception, Properties, TenantId) VALUES
                ('log-t2', 't', 'Info', '2026-06-20T10:00:00', '', '', 2),
                ('log-t3', 't', 'Info', '2026-06-20T10:01:00', '', '', 3);");

        [Fact] // LOG-19/20 — el listado heredado (EF) solo trae logs del tenant del contexto
        public async Task Listado_heredado_aisla_por_tenant()
        {
            await SeedLogsAsync();
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.GetAsync("/api/general/logInfo?page=0&pageSize=50");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("log-t2", body);
            Assert.DoesNotContain("log-t3", body); // filtro global EF excluye el de otro tenant
        }

        [Fact] // LOG-21 — detalle por id de otro tenant -> 404 (filtro EF excluye -> GetById null)
        public async Task Detalle_de_otro_tenant_da_404()
        {
            await SeedLogsAsync();
            var idT3 = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_LogInfos WHERE Message = 'log-t3'");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            var resp = await client.GetAsync($"/api/general/logInfo/{idT3}");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
            Assert.DoesNotContain("log-t3", await resp.Content.ReadAsStringAsync());
        }

        [Fact] // LOG-01/02/14 — AllTenants (SQL crudo) para root-raiz ve logs de TODOS los tenants (bypassa el filtro)
        public async Task AllTenants_root_raiz_ve_todos_los_tenants()
        {
            await SeedLogsAsync();
            using var client = await CreateAuthenticatedClientAsync(); // root @ tenant raiz

            var resp = await client.GetAsync("/api/general/logInfo/AllTenants?page=0&pageSize=50");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("log-t2", body); // tenant 2
            Assert.Contains("log-t3", body); // tenant 3 (cross-tenant por diseno)
        }

        [Fact] // LOG-03 — AllTenants por un NO-root -> 400 (BusinessValidation), NO 401/403
        public async Task AllTenants_no_root_es_rechazado()
        {
            await SeedLogsAsync();
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root

            var resp = await client.GetAsync("/api/general/logInfo/AllTenants?page=0&pageSize=50");

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantNotRoot);
        }

        [Fact] // LOG-04 — AllTenants con root en contexto SIMULADO (impersonando) -> 400 (TenantId != RootTenantId)
        public async Task AllTenants_root_simulando_es_rechazado()
        {
            await SeedLogsAsync();
            // root impersona a userb (tenant 2): el contexto pasa a tenant 2 -> el gate del visor global cae.
            var impToken = await ApiClient.ImpersonationTokenAsync(Factory, TestData.ActiveTenantId, TestData.UserBId);
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", impToken);

            var resp = await client.GetAsync("/api/general/logInfo/AllTenants?page=0&pageSize=50");

            await resp.ShouldBeBadRequest();
        }
    }
}
