using System.Net;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// BARRIDO del patrón #9/#17 en los `GetByIdAsync` override de los catálogos globales
    /// (Rol/Permiso/TipoLicencia/Parametro). Cada uno mapea la entidad con Mapperly SIN null-check →
    /// un id inexistente devolvía 500 (NPE al mapear null) en vez de 404. Son catálogos globales (sin
    /// TenantId) → el aislamiento multi-tenant es N/A; el único modo de falla es el id inexistente.
    /// Happy-path incluido para fijar el contrato del detalle. Seed: Rol 1, Permiso 1, TipoLicencia 1,
    /// Parametro 9.
    /// </summary>
    public class GetByIdInexistenteTests : IntegrationTestBase
    {
        public GetByIdInexistenteTests(TestWebApplicationFactory factory) : base(factory) { }

        [Theory] // get-by-id de un id existente -> 200 (contrato del detalle)
        [InlineData("/api/general/roles/1")]
        [InlineData("/api/general/permisos/1")]
        [InlineData("/api/general/tipos-licencia/1")]
        [InlineData("/api/general/parametros/9")]
        public async Task GetById_existente_da_200(string url)
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync(url);

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Theory] // get-by-id de un id inexistente -> 404 (no 500 por NPE al mapear null)
        [InlineData("/api/general/roles/999999")]
        [InlineData("/api/general/permisos/999999")]
        [InlineData("/api/general/tipos-licencia/999999")]
        [InlineData("/api/general/parametros/999999")]
        public async Task GetById_inexistente_da_404(string url)
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync(url);

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
