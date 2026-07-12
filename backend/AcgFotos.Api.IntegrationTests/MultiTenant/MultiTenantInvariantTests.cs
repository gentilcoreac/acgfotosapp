using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Session;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.MultiTenant
{
    /// <summary>
    /// MT-08/11 — invariantes de integridad multi-tenant del embudo central
    /// (<c>AcgFotosDbContext.StampAndGuardMultiTenant</c>). No son alcanzables por HTTP con los DTOs
    /// actuales (ninguno expone <c>TenantId</c>): son defensa en profundidad contra DTOs/flujos futuros,
    /// así que se ejercitan directo sobre el DbContext con un <see cref="IAppContext"/> controlado.
    /// </summary>
    public class MultiTenantInvariantTests : IntegrationTestBase
    {
        public MultiTenantInvariantTests(TestWebApplicationFactory factory) : base(factory) { }

        /// <summary>DbContext real (misma options/model del host) con un IAppContext de test.</summary>
        private AcgFotosDbContext CreateDbContext(IServiceScope scope, IAppContext appContext) =>
            new AcgFotosDbContext(
                scope.ServiceProvider.GetRequiredService<DbContextOptions<AcgFotosDbContext>>(),
                appContext,
                scope.ServiceProvider.GetRequiredService<ILogger<AcgFotosDbContext>>());

        [Fact] // MT-08 — el TenantId de una entidad EXISTENTE es inmutable: un update que lo cambia corta
        public async Task Update_que_cambia_el_TenantId_corta_y_no_persiste()
        {
            using var scope = Factory.Services.CreateScope();
            using var db = CreateDbContext(scope, FakeAppContext.TenantUser(TestData.ActiveTenantId));

            var user = await db.Usuarios.FirstAsync(x => x.Id == TestData.UserBId); // tenant 2
            user.TenantId = TestData.RootTenantId; // simula un SetValues(dto) con TenantId en el DTO

            await Assert.ThrowsAsync<BusinessValidationException>(() => db.SaveChangesAsync());

            var tenantId = await Factory.QueryScalarAsync<long>(
                $"SELECT TenantId FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.Equal(TestData.ActiveTenantId, tenantId); // sin re-asignar
        }

        [Fact] // MT-09 — alta con TenantId explícito de OTRO tenant por un actor no-root corta
        public async Task Alta_con_tenant_explicito_de_otro_tenant_por_no_root_corta()
        {
            using var scope = Factory.Services.CreateScope();
            using var db = CreateDbContext(scope, FakeAppContext.TenantUser(TestData.ActiveTenantId));

            db.Grupos.Add(new Grupo { Nombre = "g-mt9-cross", TenantId = TestData.RootTenantId });

            await Assert.ThrowsAsync<BusinessValidationException>(() => db.SaveChangesAsync());

            var count = await CountAsync("SELECT COUNT(*) FROM gen_Grupos WHERE Nombre = 'g-mt9-cross'");
            Assert.Equal(0, count);
        }

        [Fact] // MT-10 — DbSet.Update() tracked SIN cambiar el tenant NO es falso positivo (marca todo Modified)
        public async Task Update_tracked_sin_cambiar_tenant_guarda_ok()
        {
            using var scope = Factory.Services.CreateScope();
            using var db = CreateDbContext(scope, FakeAppContext.TenantUser(TestData.ActiveTenantId));

            var user = await db.Usuarios.FirstAsync(x => x.Id == TestData.UserBId);
            user.Nombre = "mt10-editado";
            db.Usuarios.Update(user); // patrón UpdateProfileAsync: todas las props quedan Modified

            await db.SaveChangesAsync(); // no debe cortar: el valor de TenantId no cambió

            var nombre = await Factory.QueryScalarAsync<string>(
                $"SELECT Nombre FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.Equal("mt10-editado", nombre);
        }

        [Fact] // MT-11 — alta con TenantId explícito IGUAL al del contexto pasa (equivale al estampado)
        public async Task Alta_con_tenant_explicito_propio_guarda_ok()
        {
            using var scope = Factory.Services.CreateScope();
            using var db = CreateDbContext(scope, FakeAppContext.TenantUser(TestData.ActiveTenantId));

            db.Grupos.Add(new Grupo { Nombre = "g-mt11-own", TenantId = TestData.ActiveTenantId });
            await db.SaveChangesAsync();

            var tenantId = await Factory.QueryScalarAsync<long>(
                "SELECT TenantId FROM gen_Grupos WHERE Nombre = 'g-mt11-own'");
            Assert.Equal(TestData.ActiveTenantId, tenantId);
        }

        /// <summary>
        /// IAppContext determinístico para ejercitar el guard central sin HTTP: usuario final de un
        /// tenant (no root, no anónimo, puede guardar multi-tenant).
        /// </summary>
        private sealed class FakeAppContext : IAppContext
        {
            public static FakeAppContext TenantUser(long tenantId) => new() { TenantId = tenantId };

            public string Token => "test-token"; // no vacío → IsAnonymous=false
            public bool IsAnonymous => false;
            public bool IsAdmin => false;
            public bool IsRoot => false;
            public long? ImpersonatedBy => null;
            public long UserId => TestData.UserBId;
            public string UserName => TestData.UserB;
            public long TenantId { get; private init; }
            public long? AplicacionId => null;

            public void SetContext(HttpRequest request) { }
            public void SetTenantId(long tenantId) { }
            public void SetUserId(long userId) { }
            public void SetUserAdmin(bool esAdmin) { }
            public void SetSystemContext(long tenantId, long userId, bool isAdmin) { }
            public bool AllowSaveMultiTenantEntities() => true;
            public bool CheckPemissions() => true;
        }
    }
}
