using AcgFotos.Core.Controllers;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Base de los tests de integracion (host con authz OFF). Antes de cada test resetea+siembra la base
    /// (estado aislado y conocido). Los helpers HTTP delegan en <see cref="ApiClient"/> (fuente unica,
    /// compartida con <see cref="AuthzTestBase"/>).
    /// </summary>
    [Collection(ApiTestCollection.Name)]
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly TestWebApplicationFactory Factory;

        protected IntegrationTestBase(TestWebApplicationFactory factory) => Factory = factory;

        public Task InitializeAsync() => Factory.ResetAndSeedAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        protected HttpClient CreateClient() => ApiClient.Create(Factory);

        /// <summary>Atajo de aserción sobre el estado de la DB: COUNT(*)/escalar int (reusado por todas las áreas).</summary>
        protected Task<int> CountAsync(string sql) => Factory.QueryScalarAsync<int>(sql);

        protected Task<string> LoginAsync(string userName = TestData.Root, string password = TestData.Password)
            => ApiClient.LoginAsync(Factory, userName, password);

        protected Task<HttpClient> CreateAuthenticatedClientAsync(string userName = TestData.Root, string password = TestData.Password)
            => ApiClient.AuthedAsync(Factory, userName, password);

        protected static Task<ApiErrorResponse> ReadErrorAsync(HttpResponseMessage resp) => ApiClient.ReadErrorAsync(resp);

        protected static string? ExtractCookieValue(HttpResponseMessage resp, string cookieName) => ApiClient.CookieValue(resp, cookieName);

        protected static string? GetSetCookieHeader(HttpResponseMessage resp, string cookieName) => ApiClient.SetCookieHeader(resp, cookieName);
    }
}
