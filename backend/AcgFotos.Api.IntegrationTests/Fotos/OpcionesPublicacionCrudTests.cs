using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>CRUD de opciones de publicación (ADR-15 §5): resolución/calidad, eje independiente de la marca de agua.</summary>
    public class OpcionesPublicacionCrudTests : IntegrationTestBase
    {
        public OpcionesPublicacionCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static object Input(string nombre, long id = 0, bool esDefault = false,
            int ladoMayorPreview = 900, int ladoMayorThumb = 300, int calidad = 55) => new
        {
            id,
            nombre,
            esDefault,
            ladoMayorPreview,
            ladoMayorThumb,
            calidad,
        };

        private async Task<OpcionesPublicacionDto> CreateAsync(HttpClient client, string nombre, bool esDefault = false)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/marca-agua/opciones-publicacion/update",
                Input(nombre, esDefault: esDefault));
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<OpcionesPublicacionDto>())!;
        }

        [Fact] // OPC-01 — alta persiste escalares + tenant
        public async Task Alta_persiste_opciones_y_tenant()
        {
            using var client = await CreateTenantClientAsync();

            var dto = await CreateAsync(client, "Alta calidad");

            Assert.Equal(1, await CountAsync(
                $"SELECT COUNT(*) FROM fot_OpcionesPublicacion WHERE Id = {dto.Id} AND TenantId = 2"));
        }

        [Fact] // OPC-02 — calidad fuera de rango → 400
        public async Task Calidad_fuera_de_rango_da_400()
        {
            using var client = await CreateTenantClientAsync();

            var resp = await client.PostAsJsonAsync("/api/fotos/marca-agua/opciones-publicacion/update",
                Input("Con calidad mala", calidad: 150));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_OpcionesPublicacion"));
        }

        [Fact] // OPC-03 — un solo default por tenant
        public async Task Marcar_default_desmarca_el_anterior()
        {
            using var client = await CreateTenantClientAsync();
            await CreateAsync(client, "Uno", esDefault: true);
            var dos = await CreateAsync(client, "Dos", esDefault: true);

            Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM fot_OpcionesPublicacion WHERE TenantId = 2 AND EsDefault = true"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_OpcionesPublicacion WHERE Id = {dos.Id} AND EsDefault = true"));
        }

        [Fact] // OPC-04 — aislamiento multi-tenant
        public async Task Opciones_de_otro_tenant_son_invisibles()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var dto = await CreateAsync(tenant2, "Sólo tenant 2");

            using var tenant3 = await CreateTenantClientAsync(3);

            await (await tenant3.GetAsync($"/api/fotos/marca-agua/opciones-publicacion/{dto.Id}"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // OPC-05 — delete
        public async Task Delete_borra_el_conjunto()
        {
            using var client = await CreateTenantClientAsync();
            var dto = await CreateAsync(client, "Descartable");

            var resp = await client.DeleteAsync($"/api/fotos/marca-agua/opciones-publicacion/{dto.Id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_OpcionesPublicacion WHERE Id = {dto.Id}"));
        }
    }
}
