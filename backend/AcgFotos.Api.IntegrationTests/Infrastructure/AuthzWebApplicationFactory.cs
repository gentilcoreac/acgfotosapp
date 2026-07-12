using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Variante del host con <c>AuthorizationEnabled=true</c> para ejercitar el filtro
    /// <c>EndpointAuthoritation</c> (matriz de autorizacion por endpoint, ADR-0003/0004). Hereda todo
    /// del factory base (Respawn, seed, helpers, connection strings, rate limiting off) y solo enciende
    /// el flag de autorizacion. Vive en su propia coleccion para no contaminar el cache de authz del
    /// resto de los tests (que corren con authz off).
    /// </summary>
    public class AuthzWebApplicationFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AuthorizationEnabled"] = "true",
                });
            });
        }

        /// <summary>
        /// Fija la version global de autorizacion (gen_AuthzVersion). El cache de endpoints permitidos
        /// se indexa por esta version (leida en vivo): darle un valor unico por test evita que el set
        /// cacheado de un test anterior (mismo usuario/tenant) se filtre al siguiente. Ver ADR-0003.
        /// </summary>
        public Task SetAuthzVersionAsync(long version) => ExecuteSqlAsync(
            // Id no es identity (fila singleton, PK fija): se inserta explicito tras limpiar.
            $"DELETE FROM gen_AuthzVersion; INSERT INTO gen_AuthzVersion (Id, Version) VALUES (1, {version});");
    }
}
