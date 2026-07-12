using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Export CSV del listado (USR-41/42). Reusa el mismo SearchAsync que el listado paginado, así que
    /// hereda su aislamiento multi-tenant y la exclusión de admins para el viewer no-admin: el CSV NO
    /// puede ser una vía de fuga de datos de otro tenant ni de los admins.
    /// </summary>
    public class UsuarioCsvTests : IntegrationTestBase
    {
        public UsuarioCsvTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // USR-41 — export happy path: 200, text/csv, filename de descarga
        public async Task Export_csv_happy_path()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/usuarios/csv");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            Assert.Equal("text/csv", resp.Content.Headers.ContentType?.MediaType);
            Assert.StartsWith("export_", resp.Content.Headers.ContentDisposition?.FileName);
        }

        [Fact] // USR-42 — el CSV de un no-admin respeta aislamiento (solo su tenant) y excluye admins
        public async Task Export_csv_aisla_tenant_y_excluye_admins()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-admin del tenant 2

            var resp = await client.GetAsync("/api/general/usuarios/csv");
            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var csv = await resp.Content.ReadAsStringAsync();

            Assert.Contains("userb", csv);          // su propio tenant, visible
            Assert.DoesNotContain("adminb2", csv);   // admin del tenant 2 -> excluido para el no-admin
            Assert.DoesNotContain("userc", csv);     // tenant 3 -> aislamiento
            Assert.DoesNotContain("root", csv);      // tenant 1 -> aislamiento
        }
    }
}
