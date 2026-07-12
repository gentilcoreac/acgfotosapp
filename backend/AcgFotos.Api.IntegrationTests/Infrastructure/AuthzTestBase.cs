using AcgFotos.Core.Controllers;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Base de los tests con <c>AuthorizationEnabled=true</c>. Antes de cada test resetea+siembra y le
    /// asigna una version de authz UNICA (aisla el cache de endpoints entre tests). Los helpers HTTP
    /// delegan en <see cref="ApiClient"/> (misma fuente que <see cref="IntegrationTestBase"/>).
    /// </summary>
    [Collection(AuthzTestCollection.Name)]
    public abstract class AuthzTestBase : IAsyncLifetime
    {
        // Version de authz unica y creciente por test (la base se resetea, asi que arranca limpia).
        private static long _versionSeq = 1000;

        protected readonly AuthzWebApplicationFactory Factory;

        protected AuthzTestBase(AuthzWebApplicationFactory factory) => Factory = factory;

        public async Task InitializeAsync()
        {
            await Factory.ResetAndSeedAsync();
            await Factory.SetAuthzVersionAsync(Interlocked.Increment(ref _versionSeq));
        }

        public Task DisposeAsync() => Task.CompletedTask;

        protected HttpClient CreateClient() => ApiClient.Create(Factory);

        protected Task<string> LoginAsync(string userName, string password = TestData.Password)
            => ApiClient.LoginAsync(Factory, userName, password);

        protected Task<HttpClient> CreateAuthenticatedClientAsync(string userName = TestData.Root, string password = TestData.Password)
            => ApiClient.AuthedAsync(Factory, userName, password);

        protected static Task<ApiErrorResponse> ReadErrorAsync(HttpResponseMessage resp) => ApiClient.ReadErrorAsync(resp);
    }
}
