using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Listado paginado de usuarios (USR-38/39/40): búsqueda libre server-side (SearchText sobre
    /// nombre/apellido/usuario/email), proyección del estado real de lockout (Bloqueado) y orden por
    /// la columna calculada UltimoLogin. El actor admin (adminb2) ve TODO el tenant 2 (no excluye admins).
    /// El aislamiento multi-tenant del listado ya está cubierto en MT-01.
    /// </summary>
    public class UsuarioSearchTests : IntegrationTestBase
    {
        public UsuarioSearchTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record HeaderItem(long Id, string UserName, bool Bloqueado, System.DateTime? UltimoLogin);

        [Fact] // USR-38 — SearchText filtra por nombre/apellido/usuario/email (typeahead)
        public async Task SearchText_filtra_por_coincidencia()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2); // admin t2 (ve todo el tenant)

            var page = await (await client.GetAsync("/api/general/usuarios?searchText=pending"))
                .Content.ReadFromJsonAsync<Page<HeaderItem>>();

            Assert.NotNull(page);
            Assert.NotEmpty(page!.Items);
            // Solo coincidencias del término: "pending" matchea pending; userb/adminb2 quedan fuera.
            Assert.All(page.Items, i => Assert.Equal("pending", i.UserName));
            Assert.DoesNotContain(page.Items, i => i.UserName == "userb");
        }

        [Fact] // USR-39 — el listado proyecta el estado real de lockout (LockoutEnabled && LockoutEnd futuro)
        public async Task Listado_proyecta_bloqueado_real()
        {
            // Bloqueamos a userb (LockoutEnd futuro); userb2 queda desbloqueado (LockoutEnd null).
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Usuarios SET LockoutEnd = DATEADD(year, 5, SYSDATETIMEOFFSET()) WHERE Id = {TestData.UserBId}");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var page = await (await client.GetAsync("/api/general/usuarios"))
                .Content.ReadFromJsonAsync<Page<HeaderItem>>();

            var userb = Assert.Single(page!.Items, i => i.UserName == "userb");
            Assert.True(userb.Bloqueado); // lockout vigente => bloqueado
            var userb2 = Assert.Single(page.Items, i => i.UserName == "userb2");
            Assert.False(userb2.Bloqueado); // LockoutEnd null => no bloqueado
        }

        [Fact] // USR-40 — orden por la columna calculada UltimoLogin (descendente: el login más reciente primero)
        public async Task Ordena_por_ultimo_login_descendente()
        {
            // userb logueó hoy; userb2 hace un año. Orden desc => userb antes que userb2.
            await Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_UsuariosHistorial (UsuarioId, Periodo, FechaLastLogin, TenantId) VALUES
                    ({TestData.UserBId}, 'p', SYSDATETIME(), {TestData.ActiveTenantId}),
                    ({TestData.UserB2Id}, 'p', DATEADD(year, -1, SYSDATETIME()), {TestData.ActiveTenantId});");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.GetAsync("/api/general/usuarios?orderBy=ultimoLogin&descendingOrder=true");
            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var page = await resp.Content.ReadFromJsonAsync<Page<HeaderItem>>();

            var idxUserb = page!.Items.FindIndex(i => i.UserName == "userb");
            var idxUserb2 = page.Items.FindIndex(i => i.UserName == "userb2");
            Assert.True(idxUserb >= 0 && idxUserb2 >= 0);
            Assert.True(idxUserb < idxUserb2, "userb (login reciente) debe ir antes que userb2 en orden desc");
        }
    }
}
