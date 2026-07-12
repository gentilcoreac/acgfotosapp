using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.MultiTenant
{
    /// <summary>
    /// Aislamiento multi-tenant en ESCRITURA (MT-03/04). El filtro impide editar entidades de otro
    /// tenant, y el alta hereda el TenantId del contexto (no del body — el UsuarioInputDto ni lo expone).
    /// </summary>
    public class MultiTenantWriteTests : IntegrationTestBase
    {
        public MultiTenantWriteTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // MT-03 — no se puede editar un usuario de otro tenant (queda intacto)
        public async Task Update_de_usuario_de_otro_tenant_no_lo_modifica()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            // Intenta pisar a root (Id=1, tenant 1) desde el tenant 2.
            await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                id = TestData.RootId,
                userName = "root",
                nombre = "HACKEADO",
                apellido = "HACKEADO",
                email = "admin@tech-bi.com",
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            // AISLAMIENTO: el filtro deja la entidad fuera de alcance -> root no se modifica.
            var nombre = await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Usuarios WHERE Id = {TestData.RootId}");
            Assert.Equal("root", nombre); // sin pisar
        }

        [Fact] // MT-04 — el alta hereda el TenantId del contexto (no del body; el InputDto ni lo expone)
        public async Task Alta_de_usuario_hereda_el_tenant_del_contexto()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                userName = "nuevo-mt4",
                nombre = "Nuevo",
                apellido = "MT4",
                email = "nuevo-mt4@tech-bi.com",
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            await resp.ShouldBeOk();
            var tenantId = await Factory.QueryScalarAsync<long>(
                "SELECT TenantId FROM gen_Usuarios WHERE UserName = 'nuevo-mt4'");
            Assert.Equal(TestData.ActiveTenantId, tenantId); // 2, del contexto
        }

        [Fact] // MT-06 (Usuario) — HALLAZGO #10: el alta de Usuario tiene flujo propio (Identity.CreateAsync)
        // que NO pasa por el SetAppContextTenant generico, asi que root SI crea (usuario del tenant raiz).
        // El guard ErrorTenantNotRoot (AllowSaveMultiTenantEntities=false) aplica a entidades de flujo base
        // (p. ej. Grupo) -> el caso MT-06 "puro" se cubre en el area Grupos (220).
        public async Task Root_crea_usuario_en_el_tenant_raiz()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root, tenant raiz

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update", new
            {
                userName = "nuevo-root-ctx",
                nombre = "X",
                apellido = "Y",
                email = "nuevo-root-ctx@tech-bi.com",
                roles = Array.Empty<object>(),
                usuarioAplicaciones = Array.Empty<object>(),
            });

            await resp.ShouldBeOk();
            var tenantId = await Factory.QueryScalarAsync<long>(
                "SELECT TenantId FROM gen_Usuarios WHERE UserName = 'nuevo-root-ctx'");
            Assert.Equal(TestData.RootTenantId, tenantId); // creado en el tenant raiz (1)
        }

        // NOTA: "root impersonando crea en el tenant destino" (MT-07) se cubre en MultiTenantGrupoTests
        // (entidad de flujo base, canonica para el guard). Via Usuario seria redundante con MT-04
        // (un contexto de tenant 2 crea un usuario de tenant 2): se quito para no duplicar.
    }
}
