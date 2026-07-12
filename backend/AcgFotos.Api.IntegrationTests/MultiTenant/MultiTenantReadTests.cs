using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Data;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.MultiTenant
{
    /// <summary>
    /// Aislamiento multi-tenant en LECTURA (MT-01/02/10). El filtro global de EF acota por el TenantId
    /// del claim: un usuario del tenant 2 sólo ve datos del tenant 2. Corre con authz OFF (host base):
    /// el aislamiento es del filtro de tenant, independiente de la autorizacion por endpoint.
    /// </summary>
    public class MultiTenantReadTests : IntegrationTestBase
    {
        public MultiTenantReadTests(TestWebApplicationFactory factory) : base(factory) { }

        // Forma minima de la respuesta (evita acoplar a los tipos exactos del proyecto).
        private sealed record UsuarioRow(string UserName, string Email);
        private sealed record Paginated(IReadOnlyList<UsuarioRow> Items, int TotalCount);

        [Fact] // MT-01 / MT-10 — el listado de usuarios sólo trae los del tenant del contexto
        public async Task Listado_de_usuarios_solo_trae_los_del_tenant_del_contexto()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.GetAsync("/api/general/usuarios");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var page = await resp.Content.ReadFromJsonAsync<Paginated>();
            var names = page!.Items.Select(i => i.UserName).ToList();

            Assert.Contains("userb", names);
            Assert.Contains("userb2", names);
            Assert.Contains("pending", names);       // pending tambien es del tenant 2
            // nada de otros tenants:
            Assert.DoesNotContain("root", names);    // tenant 1
            Assert.DoesNotContain("adminb", names);  // tenant 3
            Assert.DoesNotContain("userc", names);   // tenant 3
            Assert.Equal(3, page.TotalCount);        // tenant 2 = userb + userb2 + pending
        }

        [Fact] // MT-02 — no se puede leer por Id un usuario de otro tenant (no filtra datos)
        public async Task Get_por_id_de_usuario_de_otro_tenant_no_expone_datos()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            // root (Id=1) pertenece al tenant 1: el filtro lo oculta.
            var resp = await client.GetAsync($"/api/general/usuarios/{TestData.RootId}");

            // AISLAMIENTO + manejo de error: entidad filtrada por tenant -> null -> 404 (no 500 ni datos).
            // HALLAZGO #9 CORREGIDO: antes UsuarioAppService.GetByIdAsync mapeaba la entidad NULL
            // (_usuarioMapper.ToDto(null) -> NPE -> 500); ahora hace null-check -> NotFound.
            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.DoesNotContain("admin@tech-bi.com", body); // no filtra el email de root
        }

        [Fact] // USR-34 — get por Id inexistente -> 404 (mismo null-check, sin NPE)
        public async Task Get_por_id_inexistente_da_404()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/usuarios/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // MT-02 (variante) — el propio tenant SÍ se lee por Id
        public async Task Get_por_id_del_propio_tenant_devuelve_200()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.GetAsync($"/api/general/usuarios/{TestData.UserB2Id}"); // userb2, tenant 2

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // MT-09 — contexto anonimo (sin request) deshabilita el filtro: ve TODOS los tenants
        public async Task Contexto_anonimo_ve_todos_los_tenants()
        {
            // Scope sin HttpContext => AppContext.IsAnonymous => el filtro `IsAnonymous || TenantId==ctx`
            // corto-circuita y devuelve filas de todos los tenants. GOTCHA: cualquier endpoint
            // AllowAnonymous que lea entidades multi-tenant expondria datos de todos los tenants.
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcgFotosDbContext>();

            var total = await db.Set<Usuario>().CountAsync();

            // Seed: root(1) + userb(10) + adminb(11) + userc(12) + pending(13) + userb2(14) + adminb2(15) = 7, de 3 tenants.
            Assert.Equal(7, total);
        }
    }
}
