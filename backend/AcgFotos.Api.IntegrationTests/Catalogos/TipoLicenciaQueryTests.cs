using System.Collections.Generic;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Tipos de licencia (190) — listado proyectado en SQL (header liviano) con búsqueda por Descripcion o
    /// CodigoTipoLicencia. Catálogo global → actor root, aislamiento N/A. El paginado es 0-based (sin query
    /// usa la 1ª página). Seed: tipoLic 1 (Visualizador, ofDataReader), 2 (Admin del Tenant Cliente, adminCliente).
    /// </summary>
    public class TipoLicenciaQueryTests : IntegrationTestBase
    {
        public TipoLicenciaQueryTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // TLI-01 — listado paginado happy: PaginationSet<TipoLicenciaHeaderDto> con los del seed
        public async Task Listado_paginado_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/tipos-licencia");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<TipoLicenciaHeaderDto>>();
            Assert.True(page!.TotalCount >= 2);
            Assert.Contains(page.Items, t => t.Descripcion == "Visualizador" && t.CodigoTipoLicencia == "ofDataReader");
        }

        [Fact] // TLI-05 — SearchText filtra por Descripcion en SQL
        public async Task SearchText_filtra_por_descripcion()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/tipos-licencia?searchText=Visual");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<TipoLicenciaHeaderDto>>();
            Assert.Contains(page!.Items, t => t.Descripcion == "Visualizador");
            Assert.DoesNotContain(page.Items, t => t.Descripcion == "Admin del Tenant Cliente");
        }

        [Fact] // TLI-06 — SearchText también filtra por CodigoTipoLicencia (OR Descripcion)
        public async Task SearchText_filtra_por_codigo()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/tipos-licencia?searchText=ofData");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<TipoLicenciaHeaderDto>>();
            Assert.Contains(page!.Items, t => t.CodigoTipoLicencia == "ofDataReader"); // match por código
            Assert.DoesNotContain(page.Items, t => t.CodigoTipoLicencia == "adminCliente");
        }

        [Fact] // TLI-07 — SearchText sin coincidencias: lista vacía
        public async Task SearchText_sin_coincidencias_vacio()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/tipos-licencia?searchText=zzzznoexiste");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<TipoLicenciaHeaderDto>>();
            Assert.Equal(0, page!.TotalCount);
            Assert.Empty(page.Items);
        }

        [Fact] // TLI-08 — el header NO expone los roles licenciados (shape plano por proyección SQL)
        public async Task Listado_no_expone_roles()
        {
            using var client = await CreateAuthenticatedClientAsync();
            // tipoLic 1 con un rol licenciado (gen_TipoLicenciaRoles) para verificar que igual no viaja en el header.
            await Factory.ExecuteSqlAsync("INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1)");

            var resp = await client.GetAsync("/api/general/tipos-licencia");
            await resp.ShouldBeOk();

            var raw = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Visualizador", raw);
            Assert.DoesNotContain("tipoLicenciaRoles", raw, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("rolId", raw, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
