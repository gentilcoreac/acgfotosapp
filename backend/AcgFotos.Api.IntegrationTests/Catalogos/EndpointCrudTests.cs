using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Endpoints (250) — ABM del catálogo de endpoints (gen_EndPoints, GLOBAL → aislamiento MT N/A) heredado
    /// del CRUD base (EndpointController : EntityApiControllerBase). El /discover (repoblado desde el descriptor
    /// provider) ya está cubierto en authz 130 (AUTHZ-11/12/14/15/16). Acá: listado/búsqueda (SearchByMatchAsync
    /// filtra por Module/Controller/Action, NO por Route/HttpMethod), detalle, alta/edición (SetValues copia
    /// TODOS los escalares incl. Activo/Route/HttpMethod), índice único (Route,HttpMethod), delete con cascada,
    /// árbol jerárquico (módulo &gt; controller &gt; endpoint). Seed: gen_EndPoints VACÍO → se siembra por API.
    /// </summary>
    public class EndpointCrudTests : IntegrationTestBase
    {
        public EndpointCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<long> CreateEndpointAsync(HttpClient client, string route, string httpMethod = "GET",
            string moduleName = "Custom", string controllerName = "FooController", string actionName = "Bar",
            bool activo = true)
        {
            var resp = await client.PostAsJsonAsync("/api/general/endpoints/update",
                Payloads.EndpointInput(route, httpMethod, moduleName: moduleName, controllerName: controllerName,
                    actionName: actionName, activo: activo));
            await resp.ShouldBeOk();
            return await Factory.QueryScalarAsync<long>(
                $"SELECT Id FROM gen_EndPoints WHERE Route = '{route}' AND HttpMethod = '{httpMethod}'");
        }

        [Fact] // END-01 — listado paginado happy: PaginationSet<EndpointDto> con la firma del endpoint
        public async Task Listado_paginado_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            await CreateEndpointAsync(client, "api/general/foo/bar", controllerName: "FooController");

            var resp = await client.GetAsync("/api/general/endpoints");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<EndpointDto>>();
            Assert.Contains(page!.Items, e => e.Route == "api/general/foo/bar" && e.ControllerName == "FooController");
        }

        [Fact] // END-02 — SearchText filtra por Module/Controller/Action en SQL
        public async Task SearchText_filtra_por_module_controller_action()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateEndpointAsync(client, "api/general/t1", controllerName: "TenantController");
            await CreateEndpointAsync(client, "api/general/r1", controllerName: "RolController");

            var resp = await client.GetAsync("/api/general/endpoints?searchText=Tenant");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<EndpointDto>>();
            Assert.Contains(page!.Items, e => e.ControllerName == "TenantController");
            Assert.DoesNotContain(page.Items, e => e.ControllerName == "RolController");
        }

        [Fact] // END-03 — GOTCHA: SearchText NO matchea por Route ni HttpMethod (solo Module/Controller/Action)
        public async Task SearchText_no_matchea_route()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateEndpointAsync(client, "api/general/tenants", controllerName: "FooController", actionName: "List");

            // "tenants" está en la Route pero NO en Module/Controller/Action → no debe aparecer.
            var resp = await client.GetAsync("/api/general/endpoints?searchText=tenants");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<EndpointDto>>();
            Assert.DoesNotContain(page!.Items, e => e.Route == "api/general/tenants");
        }

        [Fact] // END-07 — detalle por id happy (Descripcion computada)
        public async Task Detalle_por_id_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/det", controllerName: "DetController", actionName: "Get");

            var resp = await client.GetAsync($"/api/general/endpoints/{id}");
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<EndpointDto>();
            Assert.Equal("api/general/det", dto!.Route);
            Assert.Contains("DetController", dto.Descripcion); // computada
        }

        [Fact] // END-08 — detalle id inexistente: 404 (GetByIdAsync null → NotFound)
        public async Task Detalle_id_inexistente_da_404()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.GetAsync("/api/general/endpoints/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // END-10 — alta happy: fila nueva con Activo=1
        public async Task Alta_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/nuevo", actionName: "Crear");

            Assert.True(id > 0);
            Assert.Equal(1, await CountAsync($"SELECT CAST(Activo AS int) FROM gen_EndPoints WHERE Id = {id}"));
        }

        [Fact] // END-11 — edición (Id!=0) actualiza la misma fila, no crea duplicado
        public async Task Edicion_actualiza_misma_fila()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/ed", actionName: "Viejo");

            var resp = await client.PostAsJsonAsync("/api/general/endpoints/update",
                Payloads.EndpointInput("api/general/ed", id: id, actionName: "Nuevo"));
            await resp.ShouldBeOk();

            Assert.Equal("Nuevo", await Factory.QueryScalarAsync<string>($"SELECT ActionName FROM gen_EndPoints WHERE Id = {id}"));
            Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM gen_EndPoints WHERE Route = 'api/general/ed'")); // sin duplicar
        }

        [Fact] // END-14 — el update SÍ aplica Activo (SetValues copia todos los escalares; NO está protegido)
        public async Task Update_aplica_activo_del_body()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/act", activo: true);

            var resp = await client.PostAsJsonAsync("/api/general/endpoints/update",
                Payloads.EndpointInput("api/general/act", id: id, activo: false));
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT CAST(Activo AS int) FROM gen_EndPoints WHERE Id = {id}")); // se apagó
        }

        [Fact] // END-15 — Route/HttpMethod son editables por el DTO (gotcha: rompería el match de authz hasta el próximo discover)
        public async Task Update_edita_route_y_httpmethod()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/sig", "GET");

            var resp = await client.PostAsJsonAsync("/api/general/endpoints/update",
                Payloads.EndpointInput("api/general/sig-new", "POST", id: id));
            await resp.ShouldBeOk();

            Assert.Equal("api/general/sig-new", await Factory.QueryScalarAsync<string>($"SELECT Route FROM gen_EndPoints WHERE Id = {id}"));
            Assert.Equal("POST", await Factory.QueryScalarAsync<string>($"SELECT HttpMethod FROM gen_EndPoints WHERE Id = {id}"));
        }

        [Fact] // END-21 — alta que duplica (Route,HttpMethod): falla controlada por uk_endpoint_route_method, no se inserta
        public async Task Alta_duplicada_route_method_falla()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateEndpointAsync(client, "api/general/dup", "GET");

            var resp = await client.PostAsJsonAsync("/api/general/endpoints/update",
                Payloads.EndpointInput("api/general/dup", "GET", controllerName: "OtroController"));

            Assert.False(resp.IsSuccessStatusCode);
            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
            Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM gen_EndPoints WHERE Route = 'api/general/dup' AND HttpMethod = 'GET'"));
        }

        [Fact] // END-17 — CheckInputValidations ahora se invoca desde ExtendedEntityAppServiceBase.UpdateAsync
               // (antes el EndpointDtoValidator existía pero nunca corría en ningún ABM del módulo general)
               // → un alta con ModuleName="" es rechazada con 400 y no se persiste.
        public async Task Alta_modulo_vacio_es_rechazada()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/endpoints/update",
                Payloads.EndpointInput("api/general/sinmod", moduleName: ""));

            Assert.False(resp.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_EndPoints WHERE Route = 'api/general/sinmod'"));
        }

        [Fact] // END-23 — delete happy
        public async Task Delete_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/del");

            var resp = await client.DeleteAsync($"/api/general/endpoints/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_EndPoints WHERE Id = {id}"));
        }

        [Fact] // END-24 — delete cascada a gen_PermisoEndpoints (revoca el mapeo de authz de esos permisos)
        public async Task Delete_cascada_a_permisoendpoints()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var id = await CreateEndpointAsync(client, "api/general/delcasc");
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_PermisoEndpoints (PermisoId, EndpointId) VALUES (1, {id})");

            var resp = await client.DeleteAsync($"/api/general/endpoints/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_EndPoints WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_PermisoEndpoints WHERE EndpointId = {id}"));
        }

        [Fact] // END-31/32 — árbol jerárquico: módulo > controller > endpoint (Name = "HttpMethod - Route")
        public async Task Hierarchical_estructura_tres_niveles()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateEndpointAsync(client, "api/general/m/foo1", moduleName: "ModA", controllerName: "FooController", actionName: "A1");
            await CreateEndpointAsync(client, "api/general/m/foo2", moduleName: "ModA", controllerName: "FooController", actionName: "A2");

            var resp = await client.GetAsync("/api/general/endpoints/hierarchical-items");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("ModA", raw);                       // nivel módulo
            Assert.Contains("FooController", raw);              // nivel controller
            Assert.Contains("GET - api/general/m/foo1", raw);   // nivel endpoint (Name)
        }

        [Fact] // END-33 — árbol con catálogo vacío: lista vacía (gen_EndPoints sin filas en el seed)
        public async Task Hierarchical_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/endpoints/hierarchical-items");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Equal("[]", raw.Trim());
        }

        [Fact] // END-36 — el árbol NO filtra por Activo: un endpoint inactivo también aparece
        public async Task Hierarchical_incluye_inactivos()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await CreateEndpointAsync(client, "api/general/inactivo", moduleName: "ModInact", activo: false);

            var resp = await client.GetAsync("/api/general/endpoints/hierarchical-items");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("ModInact", raw); // GetAllAsync no filtra por Activo
        }
    }
}
