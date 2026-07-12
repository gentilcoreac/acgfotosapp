using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Base.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Roles (160) — GET /roles/del-tenant: roles que el tenant del contexto puede otorgar según sus
    /// licencias contratadas (TenantLicencias → TipoLicencia → gen_TipoLicenciaRoles), con los chips de
    /// esas licencias. Lo usa el ABM de grupos. El resultado depende del AppContext.TenantId, no de un
    /// Id del body → la propiedad clave es que cada contexto ve SOLO los roles de SUS licencias.
    /// Arrange: Rol 1 (Administrador) → tipoLic 1 (Visualizador); "RolSoloAdmin" → tipoLic 2 (Admin).
    /// Tenant 2 (userb) tiene TenantLicencia de tipoLic 1 únicamente; root (tenant 1) no tiene ninguna.
    /// </summary>
    public class RolDelTenantTests : IntegrationTestBase
    {
        public RolDelTenantTests(TestWebApplicationFactory factory) : base(factory) { }

        // Siembra gen_TipoLicenciaRoles y un rol que SOLO pertenece a la licencia Admin (no contratada por t2).
        private async Task<long> ArrangeLicenciasYRolesAsync()
        {
            using var root = await CreateAuthenticatedClientAsync();
            var resp = await root.PostAsJsonAsync("/api/general/roles/update", Payloads.RolInput("RolSoloAdmin"));
            await resp.ShouldBeOk();
            long rolSoloAdminId = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Roles WHERE Descripcion = 'RolSoloAdmin'");

            await Factory.ExecuteSqlAsync(
                "INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES (1, 1);" +          // Administrador -> Visualizador
                $"INSERT INTO gen_TipoLicenciaRoles (RolId, TipoLicenciaId) VALUES ({rolSoloAdminId}, 2)"); // RolSoloAdmin -> Admin
            return rolSoloAdminId;
        }

        private static async Task<List<RolDelTenantDto>> GetDelTenantAsync(HttpClient client)
        {
            var resp = await client.GetAsync("/api/general/roles/del-tenant");
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<List<RolDelTenantDto>>())!;
        }

        [Fact] // ROL-44/45 — solo roles de licencias contratadas + chips de esas licencias (excluye las ajenas)
        public async Task Trae_solo_roles_de_licencias_contratadas_con_chips()
        {
            await ArrangeLicenciasYRolesAsync();

            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // tenant 2, licencia Visualizador
            var roles = await GetDelTenantAsync(client);

            var admin = Assert.Single(roles, r => r.Id == 1);
            Assert.Equal("Administrador", admin.Descripcion);
            var chip = Assert.Single(admin.Licencias);               // solo la licencia del tenant
            Assert.Equal("Visualizador", chip.Descripcion);
            Assert.DoesNotContain(roles, r => r.Descripcion == "RolSoloAdmin"); // licencia no contratada por t2
        }

        [Fact] // ROL-46 — tenant sin licencias contratadas: lista vacía (root, tenant 1)
        public async Task Tenant_sin_licencias_vacio()
        {
            await ArrangeLicenciasYRolesAsync();

            using var client = await CreateAuthenticatedClientAsync(); // root, tenant 1 sin TenantLicencias
            var roles = await GetDelTenantAsync(client);

            Assert.Empty(roles);
        }

        [Fact] // ROL-48 — root impersonando usa el TenantId del contexto simulado (t2), no el tenant raíz
        public async Task Root_impersonando_usa_tenant_del_contexto()
        {
            await ArrangeLicenciasYRolesAsync();

            var token = await ApiClient.ImpersonationTokenAsync(Factory, TestData.ActiveTenantId, TestData.UserBId);
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var roles = await GetDelTenantAsync(client);

            // Usa el tenant 2 (licencia Visualizador) → ve el Administrador; si usara el tenant raíz sería vacío.
            Assert.Contains(roles, r => r.Id == 1);
        }
    }
}
