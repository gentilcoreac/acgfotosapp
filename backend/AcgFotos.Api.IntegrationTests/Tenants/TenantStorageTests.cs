using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — subida de archivos por storage (I/O real en el host de tests, IStorageProvider):
    /// logo (Base64+Extension → images/) y CSS custom (byte[] → themes/, setea StyleSheetCssUrl). El doble-save
    /// re-persiste la URL resuelta. Actor root (root del tenant raíz; el save sin CSS regeneraría → EnsureRootCaller,
    /// por eso en TEN-63 se manda StyleSheetCssUrl para que NO regenere y aislar el caso al logo).
    /// </summary>
    public class TenantStorageTests : IntegrationTestBase
    {
        public TenantStorageTests(TestWebApplicationFactory factory) : base(factory) { }

        private static readonly object Licencia1 =
            new { tipoLicenciaId = 1L, cantidad = 5L, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" };

        [Fact] // TEN-63 — subir logo (Base64): SaveFile a images/ y la URL queda PERSISTIDA (doble-save)
        public async Task Subir_logo_persiste_url()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", new
            {
                id = TestData.ActiveTenantId,
                codigo = "tenant-b",
                nombre = "Tenant B",
                tituloWeb = "Tenant B",
                activo = true,
                styleSheetCssUrl = "http://x/custom.css", // evita la regeneración: aísla el caso al logo
                logoLoginDarkFile = new { base64 = "AAAA", extension = "png" },
                tenantLicenses = new[] { Licencia1 },
            });
            await resp.ShouldBeOk();

            var url = await Factory.QueryScalarAsync<string>(
                $"SELECT LogoLoginDarkUrl FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}");
            Assert.False(string.IsNullOrEmpty(url)); // la URL del logo subido quedó persistida
        }

        [Fact] // TEN-65 — subir CSS custom (byte[]): va a themes/ y setea StyleSheetCssUrl; NO regenera el theme base
        public async Task Subir_css_setea_stylesheet_url()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", new
            {
                id = TestData.ActiveTenantId,
                codigo = "tenant-b",
                nombre = "Tenant B",
                tituloWeb = "Tenant B",
                activo = true,
                styleSheetFile = new byte[] { 1, 2, 3, 4 }, // se serializa como base64; el server lo decodifica a byte[]
                tenantLicenses = new[] { Licencia1 },
            });
            await resp.ShouldBeOk();

            var url = await Factory.QueryScalarAsync<string>(
                $"SELECT StyleSheetCssUrl FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}");
            Assert.False(string.IsNullOrEmpty(url));
            Assert.Contains(".css", url!); // el CSS custom fue a themes/{codigo}.css
        }
    }
}
