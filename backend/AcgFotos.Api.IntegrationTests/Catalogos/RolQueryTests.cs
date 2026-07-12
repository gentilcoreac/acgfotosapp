using System.Collections.Generic;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Roles (160) — consultas: listado paginado (header liviano + búsqueda por Descripcion en SQL),
    /// GetRolesByTipoLicencia y GetRolesPermisos. Catálogo global → actor root, aislamiento N/A.
    /// Seed: Rol 1 (Administrador), Permiso 1, TipoLicencia 1/2; gen_TipoLicenciaRoles se siembra inline.
    /// </summary>
    public class RolQueryTests : IntegrationTestBase
    {
        public RolQueryTests(TestWebApplicationFactory factory) : base(factory) { }

        // Wrapper mínimo del PaginationSet con el DTO real (caza un rename de Descripcion).
        private async Task<long> CreateRolAsync(HttpClient client, string descripcion, long[] permisoIds)
        {
            var resp = await client.PostAsJsonAsync("/api/general/roles/update",
                Payloads.RolInput(descripcion, permisoIds: permisoIds));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Roles WHERE Descripcion = '{descripcion}'");
        }

        [Fact] // ROL-01 — listado paginado happy: PaginationSet<RolHeaderDto> con el rol del seed
        public async Task Listado_paginado_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/roles");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<RolHeaderDto>>();
            Assert.True(page!.TotalCount >= 1);
            Assert.Contains(page.Items, r => r.Descripcion == "Administrador");
        }

        [Fact] // ROL-05 — SearchText filtra por Descripcion en SQL (no devuelve todo)
        public async Task SearchText_filtra_por_descripcion()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateRolAsync(client, "Lector", new long[0]);

            var resp = await client.GetAsync("/api/general/roles?searchText=Admin");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<RolHeaderDto>>();
            Assert.Contains(page!.Items, r => r.Descripcion == "Administrador");
            Assert.DoesNotContain(page.Items, r => r.Descripcion == "Lector");
        }

        [Fact] // ROL-06 — SearchText sin coincidencias: lista vacía, no "devolver todo"
        public async Task SearchText_sin_coincidencias_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/roles?searchText=zzzznoexiste");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<RolHeaderDto>>();
            Assert.Equal(0, page!.TotalCount);
            Assert.Empty(page.Items);
        }

        [Fact] // ROL-07 — el listado NO expone RolPermisos (shape liviano Header), aunque el rol tenga permisos
        public async Task Listado_no_expone_rolpermisos()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateRolAsync(client, "RolLiviano", new long[] { 1 }); // tiene un permiso asignado

            var resp = await client.GetAsync("/api/general/roles");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("RolLiviano", raw);                                                   // el rol está en el listado
            Assert.DoesNotContain("permisoId", raw, System.StringComparison.OrdinalIgnoreCase);   // pero sin la colección de permisos
            Assert.DoesNotContain("rolPermisos", raw, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact] // ROL-38 — GetRolesByTipoLicencia: trae los roles cuyo TipoLicenciaRoles incluye la licencia
        public async Task GetRolesByTipoLicencia_happy()
        {
            await Factory.ExecuteSqlAsync("INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1)");

            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.GetAsync("/api/general/roles/GetRolesByTipoLicencia?tipoLicenciaId=1");
            await resp.ShouldBeOk();

            var roles = await resp.Content.ReadFromJsonAsync<List<RolDto>>();
            Assert.Contains(roles!, r => r.Id == 1 && r.Descripcion == "Administrador");
        }

        [Fact] // ROL-39 — GetRolesByTipoLicencia sin roles asociados: lista vacía (no null/500)
        public async Task GetRolesByTipoLicencia_sin_roles_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.GetAsync("/api/general/roles/GetRolesByTipoLicencia?tipoLicenciaId=99999");
            await resp.ShouldBeOk();

            var roles = await resp.Content.ReadFromJsonAsync<List<RolDto>>();
            Assert.Empty(roles!);
        }

        [Fact] // ROL-40 — GetRolesByTipoLicencia sin el query param: tipoLicenciaId=0 (default), 200 vacío
        public async Task GetRolesByTipoLicencia_sin_param_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.GetAsync("/api/general/roles/GetRolesByTipoLicencia");
            await resp.ShouldBeOk();

            var roles = await resp.Content.ReadFromJsonAsync<List<RolDto>>();
            Assert.Empty(roles!);
        }

        [Fact] // ROL-42 — GetRolesPermisos: 200 con el grafo rol→permisos (include RolPermisos.ThenInclude Permiso)
        public async Task GetRolesPermisos_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateRolAsync(client, "RolConGrafo", new long[] { 1 });

            var resp = await client.GetAsync("/api/general/roles/GetRolesPermisos");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("RolConGrafo", raw);
            Assert.Contains("permisoId", raw, System.StringComparison.OrdinalIgnoreCase); // el grafo trae los permisos
        }
    }
}
