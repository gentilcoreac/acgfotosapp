using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Todos los tests de integracion comparten UNA instancia del host (caro de construir) y, por
    /// definicion de coleccion de xUnit, corren en SERIE: no hay contencion sobre la base de tests
    /// (que se resetea+siembra antes de cada test). Tambien evita que el rate limiter / estado in-memory
    /// del host se pisen entre clases.
    /// </summary>
    [CollectionDefinition(Name)]
    public class ApiTestCollection : ICollectionFixture<TestWebApplicationFactory>
    {
        public const string Name = "api";
    }
}
