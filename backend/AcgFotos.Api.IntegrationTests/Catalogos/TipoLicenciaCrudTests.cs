using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Tipos de licencia (190) — catálogo GLOBAL (TipoLicencia : EntityBase → aislamiento MT N/A; lo
    /// administra root). Mismo patrón que Roles 160: alta/edición con roles licenciados hijos (RolIds →
    /// gen_TipoLicenciaRoles vía ChildCollectionSync.SyncBy), anti mass-assignment del flag admin-only
    /// EsDefaultParaNuevoTenant (no está en TipoLicenciaInputDto), delete con cascada e idempotencia.
    /// Detalle (TLI-13/14) ya en <see cref="GetByIdInexistenteTests"/> (tipos-licencia/1→200, /999999→404).
    /// Seed: TipoLicencia 1 (Visualizador, ofDataReader, EsDefault=0), 2 (Admin, adminCliente, EsDefault=1);
    /// TenantLicencias referencian tipoLic 1. Rol 1 (Administrador); roles extra se crean vía API.
    /// </summary>
    public class TipoLicenciaCrudTests : IntegrationTestBase
    {
        public TipoLicenciaCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<long> CreateTipoAsync(HttpClient client, string descripcion, long[] rolIds)
        {
            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/update",
                Payloads.TipoLicenciaInput(descripcion, rolIds: rolIds));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_TipoLicencia WHERE Descripcion = '{descripcion}'");
        }

        private async Task<long> CreateRolAsync(HttpClient client, string descripcion)
        {
            var resp = await client.PostAsJsonAsync("/api/general/roles/update", Payloads.RolInput(descripcion));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Roles WHERE Descripcion = '{descripcion}'");
        }

        [Fact] // TLI-18 — alta happy: persiste el tipo + sus roles licenciados (gen_TipoLicenciaRoles)
        public async Task Alta_persiste_tipo_y_roles()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            long rol2 = await CreateRolAsync(client, "RolTli2");

            var id = await CreateTipoAsync(client, "QA Pro", new long[] { 1, rol2 });

            Assert.True(id > 0);
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id}"));
        }

        [Fact] // TLI-19 — EsDefaultParaNuevoTenant arranca en false (no viaja en el DTO)
        public async Task Alta_esDefault_arranca_false()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateTipoAsync(client, "QA Sin Default", new long[0]);

            Assert.Equal(0, await CountAsync($"SELECT CAST(EsDefaultParaNuevoTenant AS int) FROM gen_TipoLicencia WHERE Id = {id}"));
        }

        [Fact] // TLI-20 — edición: cambia Descripcion y CodigoTipoLicencia (escalares sobre entidad tracked)
        public async Task Edicion_cambia_descripcion_y_codigo()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateTipoAsync(client, "Viejo", new long[0]);

            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/update",
                Payloads.TipoLicenciaInput("Nuevo", codigo: "NEW", id: id));
            await resp.ShouldBeOk();

            Assert.Equal("Nuevo", await Factory.QueryScalarAsync<string>($"SELECT Descripcion FROM gen_TipoLicencia WHERE Id = {id}"));
            Assert.Equal("NEW", await Factory.QueryScalarAsync<string>($"SELECT CodigoTipoLicencia FROM gen_TipoLicencia WHERE Id = {id}"));
        }

        [Fact] // TLI-24 — anti mass-assignment: EsDefaultParaNuevoTenant en el body NO escala
        public async Task Update_con_esDefault_en_body_no_escala()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateTipoAsync(client, "QA-MassAssign", new long[0]);

            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/update",
                new { id, codigoTipoLicencia = "X", descripcion = "QA-MassAssign", rolIds = new long[0], esDefaultParaNuevoTenant = true });
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT CAST(EsDefaultParaNuevoTenant AS int) FROM gen_TipoLicencia WHERE Id = {id}"));
        }

        [Fact] // TLI-30 — edición reconcilia roles por RolId: {1,r2} -> {r2,r3} (quita/conserva/agrega)
        public async Task Edicion_reconcilia_roles()
        {
            using var client = await CreateAuthenticatedClientAsync();
            long r2 = await CreateRolAsync(client, "RolTliR2");
            long r3 = await CreateRolAsync(client, "RolTliR3");
            var id = await CreateTipoAsync(client, "T-reconcile", new[] { 1L, r2 });

            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/update",
                Payloads.TipoLicenciaInput("T-reconcile", id: id, rolIds: new[] { r2, r3 }));
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id} AND RolId = 1"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id} AND RolId = {r2}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id} AND RolId = {r3}"));
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id}"));
        }

        [Fact] // TLI-33 — delete: borra el tipo y, en cascada, sus roles licenciados (OnDelete Cascade)
        public async Task Delete_cascada_a_roles()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateTipoAsync(client, "T-del", new long[] { 1 });

            var resp = await client.DeleteAsync($"/api/general/tipos-licencia/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicencia WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id}"));
        }

        [Fact] // TLI-34 — delete de tipo EN USO: la DB tiene FK con CASCADA (aunque el EF model no la declare
               // en TenantLicenciaEFConfig) → el delete NO se bloquea y limpia ambas hijas (UsuarioTipoLicencia +
               // TenantLicencias), sin huérfanos ni 500. (El catálogo predecía "FK bloquea"; la realidad es cascada
               // completa a nivel DB. Comportamiento consistente — borrar el tipo elimina sus cupos/licencias.)
        public async Task Delete_tipo_en_uso_cascada_a_hijas()
        {
            using var client = await CreateAuthenticatedClientAsync();
            Assert.True(await CountAsync("SELECT COUNT(*) FROM gen_TenantLicencias WHERE TipoLicenciaId = 1") > 0); // seed tiene cupos de tipoLic 1

            var resp = await client.DeleteAsync("/api/general/tipos-licencia/1");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_TipoLicencia WHERE Id = 1"));                  // se borró
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_UsuarioTipoLicencia WHERE TipoLicenciaId = 1")); // cascada (config EF)
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_TenantLicencias WHERE TipoLicenciaId = 1"));     // cascada (FK DB)
        }

        [Fact] // TLI-35 — delete de id inexistente: no-op idempotente (DeleteById FirstOrDefault null), no 500
        public async Task Delete_id_inexistente_no_op()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.DeleteAsync("/api/general/tipos-licencia/999999");

            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
        }

        [Fact] // TLI-55 — idempotencia: reenviar el mismo update no muta el join de roles (sync no-op)
        public async Task Reenviar_mismo_update_no_muta_el_join()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateTipoAsync(client, "T-idem", new long[] { 1 });

            var resp = await client.PostAsJsonAsync("/api/general/tipos-licencia/update",
                Payloads.TipoLicenciaInput("T-idem", id: id, rolIds: new long[] { 1 }));
            await resp.ShouldBeOk();

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TipoLicenciaRoles WHERE TipoLicenciaId = {id}"));
        }
    }
}
