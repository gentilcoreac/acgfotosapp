using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Parametros
{
    /// <summary>
    /// Parámetros (200) — ABM del catálogo GLOBAL (Parametro : EntityBase, NO multi-tenant → aislamiento N/A;
    /// el override por tenant es ParametroValorTenant, área 210). Listado con filtro por AplicacionId + búsqueda
    /// (Nombre/Descripcion), alta/edición (write path Mapperly), FK a Aplicacion (Restrict) y delete. El detalle
    /// (PAR-11/12) ya lo cubre <see cref="Catalogos.GetByIdInexistenteTests"/> (parametros/9→200, /999999→404).
    /// Seed: parametros Fwk.Email.* en la app general (1). App/parametros extra se siembran inline.
    /// </summary>
    public class ParametroCrudTests : IntegrationTestBase
    {
        public ParametroCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        // Siembra una aplicación global y devuelve su Id.
        private async Task<long> InsertAppAsync(string codigo)
        {
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_Aplicaciones (Codigo, Nombre, Activo) VALUES ('{codigo}', '{codigo}', true)");
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Aplicaciones WHERE Codigo = '{codigo}'");
        }

        private Task SeedParamAsync(string nombre, long aplicacionId, string? valor) =>
            Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Parametros (AplicacionId, PermisoId, TipoDato, Nombre, Descripcion, Valor) " +
                $"VALUES ({aplicacionId}, NULL, 3, '{nombre}', '{nombre}', {(valor == null ? "NULL" : $"'{valor}'")})");

        [Fact] // PAR-01 — listado paginado happy: ParametroDto con AplicacionNombre aplanado (Include Aplicacion)
        public async Task Listado_paginado_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/parametros");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<ParametroDto>>();
            var item = Assert.Single(page!.Items, p => p.Nombre == "Fwk.Email.From");
            Assert.Equal("General", item.AplicacionNombre); // app 1 = 'General'
        }

        [Fact] // PAR-02 — el listado filtra por AplicacionId (solo los de esa app)
        public async Task Listado_filtra_por_aplicacion()
        {
            using var client = await CreateAuthenticatedClientAsync();
            long app2 = await InsertAppAsync("app2-par");
            await SeedParamAsync("ParamApp2", app2, "x");

            var resp = await client.GetAsync($"/api/general/parametros?aplicacionId={app2}");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<ParametroDto>>();
            Assert.Contains(page!.Items, p => p.Nombre == "ParamApp2");
            Assert.DoesNotContain(page.Items, p => p.Nombre == "Fwk.Email.From"); // app 1 excluida
        }

        [Fact] // PAR-03 — GOTCHA: AplicacionId=0 se trata como "sin filtro" (devuelve todas las apps)
        public async Task Listado_aplicacion_cero_no_filtra()
        {
            using var client = await CreateAuthenticatedClientAsync();
            long app2 = await InsertAppAsync("app2-cero");
            await SeedParamAsync("ParamApp2Cero", app2, "x");

            var resp = await client.GetAsync("/api/general/parametros?aplicacionId=0");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<ParametroDto>>();
            Assert.Contains(page!.Items, p => p.Nombre == "Fwk.Email.From"); // app 1
            Assert.Contains(page.Items, p => p.Nombre == "ParamApp2Cero");   // app 2 → 0 no acota
        }

        [Fact] // PAR-04 — búsqueda por Nombre/Descripcion en SQL
        public async Task SearchText_filtra_por_nombre_descripcion()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await SeedParamAsync("OtroParametroSinMatch", 1, "x");

            var resp = await client.GetAsync("/api/general/parametros?searchText=Email");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<ParametroDto>>();
            Assert.Contains(page!.Items, p => p.Nombre == "Fwk.Email.From");
            Assert.DoesNotContain(page.Items, p => p.Nombre == "OtroParametroSinMatch");
        }

        [Fact] // PAR-15/24 — alta happy (Parametro es global → root crea sin gate de tenant)
        public async Task Alta_persiste()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root, sin simular
            var resp = await client.PostAsJsonAsync("/api/general/parametros/update",
                Payloads.ParametroInput("NuevoParam", valor: "1", aplicacionId: 1));
            await resp.ShouldBeOk();

            var id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Parametros WHERE Nombre = 'NuevoParam'");
            Assert.True(id > 0);
            Assert.Equal("1", await Factory.QueryScalarAsync<string>($"SELECT Valor FROM gen_Parametros WHERE Id = {id}"));
        }

        [Fact] // PAR-16 — edición: cambia el Valor sobre la entidad tracked
        public async Task Edicion_cambia_valor()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await SeedParamAsync("EditMe", 1, "1");
            long id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Parametros WHERE Nombre = 'EditMe'");

            var resp = await client.PostAsJsonAsync("/api/general/parametros/update",
                Payloads.ParametroInput("EditMe", valor: "5", id: id, aplicacionId: 1));
            await resp.ShouldBeOk();

            Assert.Equal("5", await Factory.QueryScalarAsync<string>($"SELECT Valor FROM gen_Parametros WHERE Id = {id}"));
        }

        [Fact] // PAR-23 — alta con PermisoId null es válida (PermisoId nullable)
        public async Task Alta_con_permiso_null_ok()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/parametros/update",
                Payloads.ParametroInput("ParamSinPermiso", aplicacionId: 1, permisoId: null));
            await resp.ShouldBeOk();

            Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM gen_Parametros WHERE Nombre = 'ParamSinPermiso' AND PermisoId IS NULL"));
        }

        [Fact] // PAR-17 — alta sin Nombre (NOT NULL): falla controlada, no 500, no persiste
        public async Task Alta_sin_nombre_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/parametros/update",
                new { id = 0, valor = "x", descripcion = "d", aplicacionId = 1, tipoDato = 3 });

            Assert.False(resp.IsSuccessStatusCode);
            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_Parametros WHERE Nombre IS NULL"));
        }

        [Fact] // PAR-21 — alta con AplicacionId inexistente: FK Restrict → falla controlada, no persiste
        public async Task Alta_aplicacion_inexistente_falla_controlada()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.PostAsJsonAsync("/api/general/parametros/update",
                Payloads.ParametroInput("ParamFkMala", aplicacionId: 999999));

            Assert.False(resp.IsSuccessStatusCode);
            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM gen_Parametros WHERE Nombre = 'ParamFkMala'"));
        }

        [Fact] // PAR-27 — delete happy
        public async Task Delete_happy()
        {
            using var client = await CreateAuthenticatedClientAsync();
            await SeedParamAsync("DelMe", 1, "x");
            long id = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Parametros WHERE Nombre = 'DelMe'");

            var resp = await client.DeleteAsync($"/api/general/parametros/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_Parametros WHERE Id = {id}"));
        }

        [Fact] // PAR-28 — delete de id inexistente: no-op idempotente (DeleteById null → no-op), no 500
        public async Task Delete_inexistente_no_op()
        {
            using var client = await CreateAuthenticatedClientAsync();
            var resp = await client.DeleteAsync("/api/general/parametros/999999");

            Assert.NotEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
        }
    }
}
