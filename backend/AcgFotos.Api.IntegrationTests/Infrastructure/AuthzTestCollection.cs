using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>Coleccion de los tests con autorizacion por endpoint encendida (host dedicado).</summary>
    [CollectionDefinition(Name)]
    public class AuthzTestCollection : ICollectionFixture<AuthzWebApplicationFactory>
    {
        public const string Name = "api-authz";
    }
}
