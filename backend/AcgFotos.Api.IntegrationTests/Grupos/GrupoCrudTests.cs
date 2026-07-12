using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Grupos
{
    /// <summary>
    /// Grupos (220) — ESCRITURA tenant-scopeada: alta con hijos, edición que reconcilia ambas colecciones
    /// (miembros por UsuarioId / roles por RolId), anti mass-assignment del TenantId, delete con cascada,
    /// y aislamiento de update/delete (no-root no toca grupos de otro tenant). La lectura/aislamiento y el
    /// guard de tenant (MT-05/06/07) ya están en GrupoIsolationTests / MultiTenantGrupoTests.
    /// Actor = adminb2 (tenant 2). Miembros válidos del tenant 2: userb(10), userb2(14). Rol global: 1.
    /// </summary>
    public class GrupoCrudTests : IntegrationTestBase
    {
        public GrupoCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<long> CreateGrupoAsync(string actor, object body, string nombre)
        {
            using var client = await CreateAuthenticatedClientAsync(actor);
            var resp = await client.PostAsJsonAsync("/api/general/grupos/update", body);
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Grupos WHERE Nombre = '{nombre}'");
        }

        [Fact] // GRP-21 — alta happy: persiste el grupo + sus 2 colecciones hijas
        public async Task Alta_con_miembros_y_roles()
        {
            var id = await CreateGrupoAsync(TestData.AdminB2,
                Payloads.Grupo("QA Grupo", usuarioIds: new[] { TestData.UserBId, TestData.UserB2Id }, rolIds: new long[] { 1 }),
                "QA Grupo");

            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM gen_UsuarioGrupos WHERE GrupoId = {id}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_GrupoRoles WHERE GrupoId = {id}"));
        }

        [Fact] // GRP-25/40 — edición reconcilia AMBAS colecciones por clave de negocio (add/remove/keep)
        public async Task Edicion_reconcilia_miembros_y_roles()
        {
            var id = await CreateGrupoAsync(TestData.AdminB2,
                Payloads.Grupo("Viejo", usuarioIds: new[] { TestData.UserBId, TestData.UserB2Id }, rolIds: new long[] { 1 }),
                "Viejo");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            // miembros {10,14} -> {14,15}: quita 10, conserva 14, agrega 15. roles {1} -> {} (vacía).
            var resp = await client.PostAsJsonAsync("/api/general/grupos/update",
                Payloads.Grupo("Nuevo", id: id, usuarioIds: new[] { TestData.UserB2Id, TestData.AdminB2Id }, rolIds: new long[0]));
            await resp.ShouldBeOk();

            Assert.Equal("Nuevo", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Grupos WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_UsuarioGrupos WHERE GrupoId = {id} AND UsuarioId = {TestData.UserBId}"));   // 10 quitado
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_UsuarioGrupos WHERE GrupoId = {id} AND UsuarioId = {TestData.UserB2Id}"));  // 14 conservado
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_UsuarioGrupos WHERE GrupoId = {id} AND UsuarioId = {TestData.AdminB2Id}")); // 15 agregado
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_GrupoRoles WHERE GrupoId = {id}"));  // roles vaciados
        }

        [Fact] // GRP-30 — anti mass-assignment: el TenantId del body no reasigna el grupo
        public async Task Update_con_tenantId_ajeno_no_reasigna()
        {
            var id = await CreateGrupoAsync(TestData.AdminB2, Payloads.Grupo("g-ma"), "g-ma");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/grupos/update",
                new { id, nombre = "g-ma", tenantId = 9999, usuarioIds = new long[0], rolIds = new long[0] });
            await resp.ShouldBeOk();

            var tenantId = await Factory.QueryScalarAsync<long>($"SELECT TenantId FROM gen_Grupos WHERE Id = {id}");
            Assert.Equal(TestData.ActiveTenantId, tenantId); // sigue en el tenant del contexto (2), no 9999
        }

        [Fact] // GRP-46 — un no-root no edita el grupo de otro tenant (aislamiento de escritura)
        public async Task Update_de_grupo_de_otro_tenant_no_muta()
        {
            var ajenoId = await CreateGrupoAsync(TestData.AdminB, Payloads.Grupo("g-ajeno"), "g-ajeno"); // tenant 3

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            await client.PostAsJsonAsync("/api/general/grupos/update",
                Payloads.Grupo("hack", id: ajenoId, usuarioIds: new long[0], rolIds: new long[0]));

            // El grupo de B no cambia (GetEntityToUpdateAsync filtra por el tenant del contexto -> null).
            Assert.Equal("g-ajeno", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Grupos WHERE Id = {ajenoId}"));
        }

        [Fact] // GRP-54 — delete cascada a miembros (UsuarioGrupos) y roles (GrupoRoles)
        public async Task Delete_cascada_a_hijos()
        {
            var id = await CreateGrupoAsync(TestData.AdminB2,
                Payloads.Grupo("g-del", usuarioIds: new[] { TestData.UserBId }, rolIds: new long[] { 1 }),
                "g-del");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.DeleteAsync($"/api/general/grupos/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_Grupos WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_UsuarioGrupos WHERE GrupoId = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_GrupoRoles WHERE GrupoId = {id}"));
        }

        [Fact] // GRP-57 — un no-root no borra el grupo de otro tenant
        public async Task Delete_de_grupo_de_otro_tenant_no_borra()
        {
            var ajenoId = await CreateGrupoAsync(TestData.AdminB, Payloads.Grupo("g-ajeno-del"), "g-ajeno-del"); // tenant 3

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2
            await client.DeleteAsync($"/api/general/grupos/{ajenoId}");

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_Grupos WHERE Id = {ajenoId}")); // sobrevive
        }

        [Fact] // GRP-16 — detalle de id inexistente: 404 controlado, no 500 (mismo null-check que #9)
        public async Task Detalle_id_inexistente_da_404()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);

            var resp = await client.GetAsync("/api/general/grupos/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
