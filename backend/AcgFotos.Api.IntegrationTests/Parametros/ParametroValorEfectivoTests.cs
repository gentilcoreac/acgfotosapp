using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Parametros
{
    /// <summary>
    /// Valor EFECTIVO de parametros por tenant+aplicacion (PVT-33/34/35/36) — el endpoint que arma
    /// `default global` + `override por tenant` para la pantalla de administracion centralizada.
    ///
    /// **Aislamiento (concern que estaba marcado como candidato a leak #17 -> NO es leak):** aunque
    /// `ParametrosPorTenantAplicacionAsync` toma el `tenantId` del query string sin scopearlo, el guard
    /// interno `GetAplicacionesPorTenantIdAsync` exige `IsRoot && TenantId==RootTenantId` y **tira 400
    /// antes de leer datos**. O sea, el endpoint es de hecho **root-only centralizado** (como el resto
    /// de la pantalla de params, ver #12): un no-root NO puede consultar el valor efectivo de NINGUN
    /// tenant (ni el propio por aca), asi que no hay cruce cross-tenant. Lo verifica `No_root_no_accede`.
    /// </summary>
    public class ParametroValorEfectivoTests : IntegrationTestBase
    {
        public ParametroValorEfectivoTests(TestWebApplicationFactory factory) : base(factory) { }

        private const long ParamPort = 9;       // Fwk.Email.Port, default "587"
        private const long ParamSmtp = 8;       // Fwk.Email.SMTPServer, default "smtp.gmail.com"
        private const long AppGeneral = 1;

        private sealed record ValorItem(
            long Id, string Nombre, string Valor, string ParametroValorDefaultValue,
            long? ParametroValorId, long? ParametroValorTenantId);

        private Task EnableAppAsync(long tenantId, long aplicacionId = AppGeneral) =>
            Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_TenantAplicaciones (TenantId, AplicacionId) VALUES ({tenantId}, {aplicacionId})");

        private Task SeedOverrideAsync(long tenantId, long parametroId, string valor) =>
            Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_ParametrosValoresTenants (Valor, TenantId, ParametroId) VALUES ('{valor}', {tenantId}, {parametroId})");

        private static async Task<List<ValorItem>> GetEfectivoAsync(HttpClient client, long tenantId, long aplicacionId = AppGeneral)
        {
            var resp = await client.GetAsync(
                $"/api/general/parametros/parametros-por-tenant-aplicacion?tenantId={tenantId}&aplicacionId={aplicacionId}");
            await resp.ShouldBeStatus(HttpStatusCode.OK);
            return await resp.Content.ReadFromJsonAsync<List<ValorItem>>() ?? new List<ValorItem>();
        }

        [Fact] // PVT-33 — el override del tenant gana al default global
        public async Task Override_del_tenant_gana_al_default()
        {
            await EnableAppAsync(TestData.ActiveTenantId);              // tenant 2 tiene la app
            await SeedOverrideAsync(TestData.ActiveTenantId, ParamPort, "55");

            using var client = await CreateAuthenticatedClientAsync();  // root (pantalla centralizada)
            var items = await GetEfectivoAsync(client, TestData.ActiveTenantId);

            var port = Assert.Single(items, i => i.Id == ParamPort);
            Assert.Equal("55", port.Valor);                            // gana el override
            Assert.Equal("587", port.ParametroValorDefaultValue);      // pero se preserva el default (reset)
            Assert.NotNull(port.ParametroValorId);                     // referencia a la fila de override
            Assert.Equal(TestData.ActiveTenantId, port.ParametroValorTenantId);
        }

        [Fact] // PVT-34 — un parametro sin override usa el default global (ParametroValorId null)
        public async Task Sin_override_usa_el_default()
        {
            await EnableAppAsync(TestData.ActiveTenantId);
            await SeedOverrideAsync(TestData.ActiveTenantId, ParamPort, "55"); // override solo del Port

            using var client = await CreateAuthenticatedClientAsync();
            var items = await GetEfectivoAsync(client, TestData.ActiveTenantId);

            var smtp = Assert.Single(items, i => i.Id == ParamSmtp);   // este NO tiene override
            Assert.Equal("smtp.gmail.com", smtp.Valor);                // default
            Assert.Null(smtp.ParametroValorId);                        // sin override
            Assert.Equal("smtp.gmail.com", smtp.ParametroValorDefaultValue);
        }

        [Fact] // PVT-35 — tenant sin la aplicacion habilitada -> lista vacia (no expone params de apps ajenas)
        public async Task Tenant_sin_la_app_devuelve_vacio()
        {
            // tenant 3 NO tiene gen_TenantAplicaciones para la app => guard aplicacion==null.
            using var client = await CreateAuthenticatedClientAsync(); // root
            var items = await GetEfectivoAsync(client, TestData.InactiveTenantId);

            Assert.Empty(items);
        }

        [Fact] // PVT-36 — el override de OTRO tenant no se aplica al valor efectivo del consultado
        public async Task Override_de_otro_tenant_no_se_aplica()
        {
            await EnableAppAsync(TestData.ActiveTenantId);                       // consulto el tenant 2
            await SeedOverrideAsync(TestData.InactiveTenantId, ParamPort, "zzz"); // override del tenant 3 (ajeno)

            using var client = await CreateAuthenticatedClientAsync(); // root
            var items = await GetEfectivoAsync(client, TestData.ActiveTenantId);

            var port = Assert.Single(items, i => i.Id == ParamPort);
            Assert.Equal("587", port.Valor);        // default del tenant 2, NO el "zzz" del 3
            Assert.Null(port.ParametroValorId);     // el tenant 2 no tiene override
        }

        [Fact] // AISLAMIENTO — el endpoint es root-only: un no-root no accede al valor efectivo de ningun tenant
        public async Task No_root_no_accede()
        {
            await EnableAppAsync(TestData.InactiveTenantId);
            await SeedOverrideAsync(TestData.InactiveTenantId, ParamPort, "secreto-t3");

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-root (tenant 2)
            // Intenta espiar el tenant 3 pasando su tenantId en el query.
            var resp = await client.GetAsync(
                $"/api/general/parametros/parametros-por-tenant-aplicacion?tenantId={TestData.InactiveTenantId}&aplicacionId={AppGeneral}");

            // El guard root-only (GetAplicacionesPorTenantIdAsync) corta antes de leer datos: la respuesta
            // es el error 400, NO una lista con datos -> no hay leak del override ajeno.
            await resp.ShouldBeBadRequest(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }
    }
}
