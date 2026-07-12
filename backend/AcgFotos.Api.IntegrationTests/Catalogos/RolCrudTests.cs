using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Roles (160) — ABM del catálogo GLOBAL (Rol NO es IMultiTenantEntityBase → el test de aislamiento
    /// multi-tenant es N/A; lo administra root). Foco: alta con permisos hijos, edición que reconcilia
    /// gen_RolPermisos por PermisoId (ChildCollectionSync.SyncBy), anti mass-assignment del flag admin-only
    /// EsDefaultParaNuevoTenant (no está en RolInputDto), delete con cascada e idempotencia del sync.
    /// El detalle (ROL-08/09) ya lo cubre <see cref="GetByIdInexistenteTests"/>; acá no se reimplementa.
    /// Actor = root (POST /roles/update no exige tenant: la entidad es global, ROL-53).
    /// Seed: Rol 1 (Administrador, EsDefault=0), Permiso 1 (PermisoRoot). Permisos extra se siembran inline.
    /// </summary>
    public class RolCrudTests : IntegrationTestBase
    {
        public RolCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        // Crea un rol vía API y devuelve su Id (Descripcion es la clave de búsqueda, única en el test).
        private async Task<long> CreateRolAsync(string descripcion, long[] permisoIds)
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                Payloads.RolInput(descripcion, permisoIds: permisoIds));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Roles WHERE Descripcion = '{descripcion}'");
        }

        // Siembra un permiso global (gen_Permisos) y devuelve su Id; identidad por CodigoPermiso (único).
        private async Task<long> InsertPermisoAsync(string codigo)
        {
            await Factory.ExecuteSqlAsync(
                $"INSERT INTO gen_Permisos (Nombre, CodigoPermiso, Descripcion, Activo, PermisoPadreId, AplicacionId, EsRestringido) " +
                $"VALUES (N'{codigo}', N'{codigo}', N'{codigo}', 1, NULL, 1, 0)");
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Permisos WHERE CodigoPermiso = '{codigo}'");
        }

        [Fact] // ROL-12 — alta happy: persiste el rol + su colección de permisos
        public async Task Alta_persiste_rol_y_permisos()
        {
            var id = await CreateRolAsync("QA Rol", new long[] { 1 });

            Assert.True(id > 0);
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id} AND PermisoId = 1"));
        }

        [Fact] // ROL-13 — edición: cambia la Descripcion (SetValues sobre la entidad tracked)
        public async Task Edicion_cambia_descripcion()
        {
            var id = await CreateRolAsync("Viejo", new long[] { 1 });

            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                Payloads.RolInput("Nuevo", id: id, permisoIds: new long[] { 1 }));
            await resp.ShouldBeOk();

            Assert.Equal("Nuevo", await Factory.QueryScalarAsync<string>($"SELECT Descripcion FROM gen_Roles WHERE Id = {id}"));
        }

        [Fact] // ROL-23 — edición reconcilia gen_RolPermisos por PermisoId: {1,p2} -> {p2,p3} (quita/conserva/agrega)
        public async Task Edicion_reconcilia_permisos()
        {
            long p2 = await InsertPermisoAsync("RolP2");
            long p3 = await InsertPermisoAsync("RolP3");
            var id = await CreateRolAsync("R-reconcile", new[] { 1, p2 });

            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                Payloads.RolInput("R-reconcile", id: id, permisoIds: new[] { p2, p3 }));
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id} AND PermisoId = 1"));   // quitado
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id} AND PermisoId = {p2}")); // conservado
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id} AND PermisoId = {p3}")); // agregado
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id}"));
        }

        [Fact] // ROL-17 — anti mass-assignment: EsDefaultParaNuevoTenant en el body NO escala (no está en RolInputDto)
        public async Task Update_con_esDefault_en_body_no_escala()
        {
            var id = await CreateRolAsync("R-massassign", new long[] { 1 });

            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                new { id, descripcion = "R-massassign", permisoIds = new long[] { 1 }, esDefaultParaNuevoTenant = true });
            await resp.ShouldBeOk();

            // El flag sensible sigue en false: SetValues por CurrentValues no toca lo que el DTO no declara.
            Assert.Equal(0, await CountAsync($"SELECT CAST(EsDefaultParaNuevoTenant AS int) FROM gen_Roles WHERE Id = {id}"));
        }

        [Fact] // ROL-26 — delete: borra el rol y, en cascada, sus filas join (OnDelete Cascade en RolEFConfig)
        public async Task Delete_cascada_a_rolpermisos()
        {
            var id = await CreateRolAsync("R-del", new long[] { 1 });

            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.DeleteAsync($"/api/general/roles/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_Roles WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id}"));
        }

        [Fact] // ROL-55 — idempotencia: reenviar el mismo update no muta el join (sync no-op, sin churn)
        public async Task Reenviar_mismo_update_no_muta_el_join()
        {
            long p2 = await InsertPermisoAsync("RolIdemP2");
            var id = await CreateRolAsync("R-idem", new[] { 1, p2 });

            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                Payloads.RolInput("R-idem", id: id, permisoIds: new[] { 1, p2 }));
            await resp.ShouldBeOk();

            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id} AND PermisoId = 1"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE RolId = {id} AND PermisoId = {p2}"));
        }

        [Fact] // ROL-14 — alta sin Descripcion (NOT NULL, IsRequired): falla controlada, no 500 crudo
        public async Task Alta_sin_descripcion_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                new { id = 0, permisoIds = new long[0] });

            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
            Assert.False(resp.IsSuccessStatusCode);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_Roles WHERE Descripcion IS NULL"));
        }
    }
}
