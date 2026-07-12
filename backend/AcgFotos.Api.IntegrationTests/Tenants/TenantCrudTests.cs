using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — ABM por root (entidad NO multi-tenant, pantalla root-only). Alta (crea tenant +
    /// usuario admin en una transacción), duplicados con rollback, anti mass-assignment de los campos
    /// sensibles (HasError/ErrorDescription, que el TenantInputDto NO expone) y edición de id inexistente.
    /// El aislamiento (#13) y tenants-for-root están en TenantIsolationTests; el tope de licencias en
    /// TenantLicenseQuotaTests. Actor = root (único caller real del ABM de tenants).
    /// </summary>
    public class TenantCrudTests : IntegrationTestBase
    {
        public TenantCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        // Alta de tenant nuevo (Id=0) con su usuario admin. Sin StyleSheetCssUrl => regenera el theme base
        // (el host de tests tiene storage + template funcional, igual que el happy de TenantLicenseQuotaTests).
        private static object AltaTenant(string codigo, string adminUserName) => new
        {
            id = 0,
            codigo,
            nombre = codigo,
            tituloWeb = codigo,
            activo = true,
            hostName = codigo + ".local",
            usuario = new
            {
                userName = adminUserName,
                nombre = "Admin",
                apellido = "Nuevo",
                email = adminUserName + "@tech-bi.com",
            },
            tenantAplicaciones = new[] { new { aplicacionId = 1L } },
            tenantLicenses = System.Array.Empty<object>(),
        };

        [Fact] // TEN-10 — alta happy: crea el tenant y su usuario admin (Administrador=true) en el tenant nuevo
        public async Task Alta_crea_tenant_y_usuario_admin()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", AltaTenant("nuevo-ten", "admin-nuevo"));

            await resp.ShouldBeOk();
            var tenantId = await Factory.QueryScalarAsync<long>("SELECT Id FROM gen_Tenants WHERE Codigo = 'nuevo-ten'");
            Assert.True(tenantId > 0);
            // El usuario admin se creó EN el tenant nuevo, con Administrador=true.
            var adminRow = await Factory.QueryScalarAsync<string>(
                "SELECT CONCAT(Administrador, '|', TenantId) FROM gen_Usuarios WHERE UserName = 'admin-nuevo'");
            Assert.Equal($"1|{tenantId}", adminRow);
        }

        [Fact] // TEN-12 — alta con Codigo ya existente -> 400 ErrorTenantExists; no crea el usuario admin
        public async Task Alta_con_codigo_duplicado_es_rechazada()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            // codigo "tenant-b" ya existe (tenant 2).
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", AltaTenant("tenant-b", "admin-dup"));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantExists);
            var adminCreado = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Usuarios WHERE UserName = 'admin-dup'");
            Assert.Equal(0, adminCreado);
        }

        [Fact] // TEN-13 — alta cuyo admin reusa datos existentes -> 400; rollback (el tenant ya insertado NO queda)
        public async Task Alta_con_admin_existente_hace_rollback()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            // codigo libre, pero el admin reusa el email/usuario de "userb" -> el alta del tenant debe revertirse.
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", AltaTenant("nuevo-roll", "userb"));

            // El guard efectivo es EmailExists (SQL crudo, sin filtro de tenant) -> ErrorEmailExists.
            // NOTA: el pre-check GetByUserNameAsync de TenantAppService corre bajo el filtro global y, con
            // root (contexto = tenant raíz), NO ve a userb (tenant 2) -> no dispara ErrorUserExists. No hay
            // hueco de integridad: UserName es UNIQUE global (índice DB) y el email lo atrapa antes igual.
            await resp.ShouldBeBadRequest(MessagesAPI.ErrorEmailExists);
            // El TransactionScope revierte el tenant ya insertado: no queda huérfano.
            var tenantHuerfano = await Factory.QueryScalarAsync<int>("SELECT COUNT(*) FROM gen_Tenants WHERE Codigo = 'nuevo-roll'");
            Assert.Equal(0, tenantHuerfano);
        }

        [Fact] // TEN-15 — anti mass-assignment: HasError/ErrorDescription del body se ignoran (se preservan de la entidad)
        public async Task Update_no_pisa_HasError_via_body()
        {
            // El sistema marcó el tenant 2 con error (flujo interno).
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_Tenants SET HasError = 1, ErrorDescription = 'fallo-interno' WHERE Id = {TestData.ActiveTenantId}");

            using var client = await CreateAuthenticatedClientAsync(); // root
            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", new
            {
                id = TestData.ActiveTenantId,
                codigo = "tenant-b",
                nombre = "Tenant B",
                tituloWeb = "Tenant B",
                activo = true,
                hasError = false,                 // intento de pisar el flag sensible por el body
                errorDescription = "borrado",     // idem
                tenantLicenses = new[] { new { tipoLicenciaId = 1L, cantidad = 5L, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" } },
            });

            await resp.ShouldBeOk();
            // HasError/ErrorDescription siguen como los dejó el flujo interno (el InputDto no los expone).
            var row = await Factory.QueryScalarAsync<string>(
                $"SELECT CONCAT(HasError, '|', ErrorDescription) FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}");
            Assert.Equal("1|fallo-interno", row);
        }

        [Fact] // TEN-17 — edición de un Id inexistente -> 400 ErrorTenantNotFound (no se trata como alta)
        public async Task Update_id_inexistente_da_ErrorTenantNotFound()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", new
            {
                id = 999999,
                codigo = "fantasma",
                nombre = "Fantasma",
                activo = true,
            });

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorTenantNotFound);
        }

        [Fact] // TEN-08 — detalle de un Id inexistente -> 404 (GetByIdAsync null-safe)
        public async Task Detalle_id_inexistente_da_404()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.GetAsync("/api/general/tenants/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
