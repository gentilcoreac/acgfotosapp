using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Permisos (170) — catálogo GLOBAL (Permiso : EntityBase → aislamiento MT N/A; la única segregación es
    /// EsRestringido, ver <see cref="PermisoRestringidoTests"/>). Foco escritura: alta/edición (UpdateAsync
    /// override carga 1 sola vez tracked con endpoints), anti mass-assignment de EsRestringido (no está en
    /// PermisoInputDto; el Update normal lo PRESERVA desde la entidad), sync de endpoints (gen_PermisoEndpoints,
    /// clear+add en edición / SyncBy con dedup en alta) y delete con cascada a RolPermisos.
    /// Seed: Permiso 1 (PermisoRoot, EsRestringido=1, AplicacionId=1). Permisos/endpoints extra se siembran inline.
    /// </summary>
    public class PermisoCrudTests : IntegrationTestBase
    {
        public PermisoCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<long> CreatePermisoAsync(HttpClient client, string codigo)
        {
            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput(codigo, codigo));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Permisos WHERE CodigoPermiso = '{codigo}'");
        }

        private async Task<long> InsertEndpointAsync(string route)
        {
            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_EndPoints (ModuleName, ControllerName, ActionName, HttpMethod, Route, Activo) " +
                $"VALUES ('general', 'Test', 'Act', 'GET', '{route}', true)");
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_EndPoints WHERE Route = '{route}'");
        }

        [Fact] // PER-17 — alta happy: persiste; EsRestringido arranca en false
        public async Task Alta_persiste_y_esRestringido_false()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var id = await CreatePermisoAsync(client, "qa_nuevo");

            Assert.True(id > 0);
            Assert.Equal(0, await CountAsync($"SELECT CAST(EsRestringido AS int) FROM gen_Permisos WHERE Id = {id}"));
        }

        [Fact] // PER-18 — alta ignora EsRestringido enviado en el body (no está en el DTO) → queda false
        public async Task Alta_ignora_esRestringido_del_body()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                new { id = 0, nombre = "qa_ma", codigoPermiso = "qa_ma", descripcion = "d", activo = true, aplicacionId = 1, esRestringido = true });
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync("SELECT CAST(EsRestringido AS int) FROM gen_Permisos WHERE CodigoPermiso = 'qa_ma'"));
        }

        [Fact] // PER-19 — edición: cambia escalares sobre la entidad tracked
        public async Task Edicion_cambia_escalares()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreatePermisoAsync(client, "qa_ed");

            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput("Editado", "qa_ed", id: id, descripcion: "d2", activo: false));
            await resp.ShouldBeOk();

            Assert.Equal("Editado", await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Permisos WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT CAST(Activo AS int) FROM gen_Permisos WHERE Id = {id}"));
        }

        [Fact] // PER-20 — el Update normal PRESERVA EsRestringido (ApplyInput lo ignora; no lo pisa el body)
        public async Task Edicion_preserva_esRestringido()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreatePermisoAsync(client, "qa_restr");
            await Factory.ExecuteSqlAsync($"UPDATE gen_Permisos SET EsRestringido = true WHERE Id = {id}");

            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                new { id, nombre = "qa_restr", codigoPermiso = "qa_restr", descripcion = "d", activo = true, aplicacionId = 1, esRestringido = false });
            await resp.ShouldBeOk();

            Assert.Equal(1, await CountAsync($"SELECT CAST(EsRestringido AS int) FROM gen_Permisos WHERE Id = {id}")); // sigue restringido
        }

        [Fact] // PER-27 — edición de id inexistente: BusinessValidationException, no 500 crudo
        public async Task Edicion_id_inexistente_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput("x", "x", id: 999999));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorGenericInternal);
        }

        [Fact] // PER-28 — sync endpoints en edición: alta de asociaciones (gen_PermisoEndpoints)
        public async Task Edicion_agrega_endpoints()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreatePermisoAsync(client, "qa_ep_add");
            long e1 = await InsertEndpointAsync("/qa/e1");
            long e2 = await InsertEndpointAsync("/qa/e2");

            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput("qa_ep_add", "qa_ep_add", id: id,
                    endpoints: new object[] { new { endpointId = e1 }, new { endpointId = e2 } }));
            await resp.ShouldBeOk();

            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = {id}"));
        }

        [Fact] // PER-29 — sync endpoints en edición: baja (clear+add) deja solo el que queda, sin huérfanos
        public async Task Edicion_quita_endpoints()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreatePermisoAsync(client, "qa_ep_rem");
            long e1 = await InsertEndpointAsync("/qa/r1");
            long e2 = await InsertEndpointAsync("/qa/r2");
            await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput("qa_ep_rem", "qa_ep_rem", id: id,
                    endpoints: new object[] { new { endpointId = e1 }, new { endpointId = e2 } }));

            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput("qa_ep_rem", "qa_ep_rem", id: id,
                    endpoints: new object[] { new { endpointId = e1 } }));
            await resp.ShouldBeOk();

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = {id}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = {id} AND EndpointId = {e1}"));
        }

        [Fact] // PER-33 — sync endpoints en ALTA usa SyncBy (HashSet) → dedup de EndpointId repetido
        public async Task Alta_dedup_endpoints_repetidos()
        {
            using var client = await CreateAuthenticatedClientAsync();
            long e1 = await InsertEndpointAsync("/qa/dup1");

            var resp = await client.PostAsJsonAsync("/api/general/permisos/update",
                Payloads.PermisoInput("qa_dup", "qa_dup",
                    endpoints: new object[] { new { endpointId = e1 }, new { endpointId = e1 } }));
            await resp.ShouldBeOk();

            var id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Permisos WHERE CodigoPermiso = 'qa_dup'");
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE PermisoId = {id}")); // 1 sola fila
        }

        [Fact] // PER-37 — delete: borra el permiso y, en cascada, sus RolPermisos (OnDelete Cascade)
        public async Task Delete_cascada_a_rolpermisos()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreatePermisoAsync(client, "qa_del");
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_RolPermisos (RolId, PermisoId) VALUES (1, {id})");

            var resp = await client.DeleteAsync($"/api/general/permisos/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_Permisos WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_RolPermisos WHERE PermisoId = {id}"));
        }
    }
}
