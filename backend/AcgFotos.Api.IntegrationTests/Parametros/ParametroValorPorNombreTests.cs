using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Parametros
{
    /// <summary>
    /// Parámetros (200) — resolución de valor por nombre (`valor-parametro-por-nombre/{nombre}`, [AllowAnonymous]).
    /// Reglas: anónimo (sin AppContext.AplicacionId) → default de la app general (Id=1); logueado → usa la app
    /// activa del header `aplicacionId`, con FALLBACK a la app general si el parámetro no existe en la app; nombre
    /// inexistente en ambas → BusinessValidationException; Valor null de un parámetro existente → null (no error).
    /// Uso típico: configurar la UI antes del login (OpcionesPaginacion, etc.).
    /// </summary>
    public class ParametroValorPorNombreTests : IntegrationTestBase
    {
        public ParametroValorPorNombreTests(TestWebApplicationFactory factory) : base(factory) { }

        // El endpoint devuelve el valor como string crudo (text/plain), no JSON: se lee directo y se
        // sacan comillas envolventes por si algún formatter las agrega.
        private static async Task<string> ReadValorAsync(HttpResponseMessage resp) =>
            (await resp.Content.ReadAsStringAsync()).Trim().Trim('"');

        private async Task<long> InsertAppAsync(string codigo)
        {
            await Factory.ExecuteSqlAsync($"INSERT INTO gen_Aplicaciones (Codigo, Nombre, Activo) VALUES (N'{codigo}', N'{codigo}', 1)");
            return await Factory.QueryScalarAsync<long>($"SELECT Id FROM gen_Aplicaciones WHERE Codigo = '{codigo}'");
        }

        private Task SeedParamAsync(string nombre, long aplicacionId, string? valor) =>
            Factory.ExecuteSqlAsync(
                "INSERT INTO gen_Parametros (AplicacionId, PermisoId, TipoDato, Nombre, Descripcion, Valor) " +
                $"VALUES ({aplicacionId}, NULL, 3, N'{nombre}', N'{nombre}', {(valor == null ? "NULL" : $"N'{valor}'")})");

        [Fact] // PAR-31 — ANÓNIMO: devuelve el default de la app general (AppContext.AplicacionId null → AppGeneralId=1)
        public async Task Anonimo_devuelve_default_general()
        {
            using var client = CreateClient(); // sin token

            var resp = await client.GetAsync("/api/general/parametros/valor-parametro-por-nombre/Fwk.Email.From");
            await resp.ShouldBeOk();

            Assert.Equal("jkaiter@tech-bi.com", await ReadValorAsync(resp));
        }

        [Fact] // PAR-32 — LOGUEADO: usa el valor de la app activa del header `aplicacionId` (no el global)
        public async Task Logueado_usa_aplicacion_del_header()
        {
            long app2 = await InsertAppAsync("app2-vpn");
            await SeedParamAsync("OpcionPag", 1, "10");      // app general
            await SeedParamAsync("OpcionPag", app2, "50");   // app activa

            using var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("aplicacionId", app2.ToString());

            var resp = await client.GetAsync("/api/general/parametros/valor-parametro-por-nombre/OpcionPag");
            await resp.ShouldBeOk();

            Assert.Equal("50", await ReadValorAsync(resp)); // valor de la app del header, no el general
        }

        [Fact] // PAR-33 — fallback: si el parámetro NO existe en la app activa, cae al de la app general
        public async Task Fallback_a_app_general()
        {
            long app2 = await InsertAppAsync("app2-fb");
            await SeedParamAsync("SoloGeneral", 1, "valor-general"); // solo en app general

            using var client = await CreateAuthenticatedClientAsync();
            client.DefaultRequestHeaders.Add("aplicacionId", app2.ToString()); // app sin el parámetro

            var resp = await client.GetAsync("/api/general/parametros/valor-parametro-por-nombre/SoloGeneral");
            await resp.ShouldBeOk();

            Assert.Equal("valor-general", await ReadValorAsync(resp)); // fallback al general
        }

        [Fact] // PAR-34 — nombre inexistente en app activa Y general → BusinessValidationException (ErrorParamNotExists)
        public async Task Inexistente_da_error_de_negocio()
        {
            using var client = await CreateAuthenticatedClientAsync();

            var resp = await client.GetAsync("/api/general/parametros/valor-parametro-por-nombre/NoExisteNiAhi");

            await resp.ShouldBeBadRequest(string.Format(MessagesAPI.ErrorParamNotExists, "NoExisteNiAhi"));
        }

        [Fact] // PAR-35 — parámetro existente con Valor null → 200 null (no confunde "existe con null" con "no existe")
        public async Task Valor_null_no_es_error()
        {
            await SeedParamAsync("ParamVacio", 1, null);

            using var client = CreateClient(); // anónimo → app general
            var resp = await client.GetAsync("/api/general/parametros/valor-parametro-por-nombre/ParamVacio");
            await resp.ShouldBeOk();

            var valor = await ReadValorAsync(resp);
            Assert.True(string.IsNullOrEmpty(valor) || valor == "null"); // null/"" sin ErrorParamNotExists
        }
    }
}
