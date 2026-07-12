using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — tope de licencias al editar un tenant (ValidateTenantLicensesAsync, LIC-44/45).
    /// Regla tenant-scopeada: root no puede reducir el cupo de un tipo por debajo de las licencias ya
    /// ASIGNADAS y vigentes de ese tenant. El cómputo del uso usa solo licencias activas (onlyActive).
    /// Seed tenant 2: TenantLicencia tipo1 Cantidad=5; 3 asignaciones activas (userb/userb2/adminb2).
    /// </summary>
    public class TenantLicenseQuotaTests : IntegrationTestBase
    {
        public TenantLicenseQuotaTests(TestWebApplicationFactory factory) : base(factory) { }

        // Cuerpo de edición del tenant 2 con el cupo de tipo1 fijado en `cantidad`.
        private static object EditarTenant2(long cantidadTipo1, string tituloWeb = "Tenant B") => new
        {
            id = TestData.ActiveTenantId,
            codigo = "tenant-b",
            nombre = "Tenant B",
            tituloWeb,
            activo = true,
            tenantLicenses = new[]
            {
                new { tipoLicenciaId = 1L, cantidad = cantidadTipo1, startDateTime = "2024-01-01", expireDateTime = "2099-12-31" }
            },
        };

        [Fact] // LIC-44 — reducir el cupo por debajo de lo asignado (3) -> 400 ErrorLicenseAmountAssign; no persiste
        public async Task Reducir_cupo_bajo_lo_asignado_es_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root administra cualquier tenant

            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", EditarTenant2(cantidadTipo1: 2));

            // El mensaje formatea la cantidad a eliminar = asignadas(3) - pedido(2) = 1.
            await resp.ShouldBeBadRequest(string.Format(MessagesAPI.ErrorLicenseAmountAssign, 1));
            // La validación corre antes del save -> el cupo en DB sigue en 5.
            var cupo = await Factory.QueryScalarAsync<long>(
                $"SELECT Cantidad FROM gen_TenantLicencias WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1");
            Assert.Equal(5, cupo);
        }

        [Fact] // LIC-45 — mantener (o subir) el cupo no es rechazado por el tope (happy path de edición)
        public async Task Mantener_cupo_no_es_rechazado()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root

            var resp = await client.PostAsJsonAsync("/api/general/tenants/update", EditarTenant2(cantidadTipo1: 5, tituloWeb: "Editado"));

            await resp.ShouldBeOk();
            var titulo = await Factory.QueryScalarAsync<string>(
                $"SELECT TituloWeb FROM gen_Tenants WHERE Id = {TestData.ActiveTenantId}");
            Assert.Equal("Editado", titulo); // la edición se persistió (3 asignadas <= 5 pedido => sin tope)
        }
    }
}
