using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Asignación de licencias en el alta (USR-26): el cupo del tenant es tope duro. Reusa Payloads
    /// (DRY). Actor = adminb2 (su licencia ya está asignada/activa: el cupo lleno no afecta su login).
    /// </summary>
    public class UsuarioLicenseTests : IntegrationTestBase
    {
        public UsuarioLicenseTests(TestWebApplicationFactory factory) : base(factory) { }

        [Fact] // USR-25 — alta con licencia y cupo disponible -> 200 + asignación activa para el nuevo user
        public async Task Alta_con_licencia_y_cupo_ok_asigna_la_licencia()
        {
            // Tenant 2: cupo TLI=1 = 5, asignadas 3 (userb/userb2/adminb2) => 2 disponibles.
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(userName: "nuevo-lic", licencia: Payloads.License(tipoLicenciaId: 1)));

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            // La licencia se asignó al usuario recién creado (UsuarioId resuelto post-alta), activa.
            var asignada = await Factory.QueryScalarAsync<int>($@"
                SELECT COUNT(*) FROM gen_UsuarioTipoLicencia l
                JOIN gen_Usuarios u ON u.Id = l.UsuarioId
                WHERE u.UserName = 'nuevo-lic' AND l.TipoLicenciaId = 1 AND l.IsActive = true AND l.TenantId = {TestData.ActiveTenantId}");
            Assert.Equal(1, asignada);
        }

        [Fact] // USR-26 — asignar una licencia con el cupo del tenant agotado -> 400
        public async Task Alta_con_cupo_de_licencia_agotado_devuelve_400()
        {
            // Tenant 2 tiene 3 licencias TLI=1 activas (userb, userb2, adminb2). Igualamos el cupo a 3
            // => CantidadDisponible = 0 para TLI=1.
            await Factory.ExecuteSqlAsync(
                $"UPDATE gen_TenantLicencias SET Cantidad = 3 WHERE TenantId = {TestData.ActiveTenantId} AND TipoLicenciaId = 1");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(userName: "nuevo-cupo", licencia: Payloads.License(tipoLicenciaId: 1)));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorLicensesAmount);
            var creado = await Factory.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM gen_Usuarios WHERE UserName = 'nuevo-cupo'");
            Assert.Equal(0, creado); // no se creó el usuario
        }

        [Fact] // USR-27 — asignar una licencia vencida (ExpireDatetime pasada) -> 400 ErrorLicenseExpired
        public async Task Alta_con_licencia_vencida_devuelve_400()
        {
            // Cupo del tenant 2 para TLI=2 con cupo disponible pero VENCIDO -> IsExpired manda antes que cupo.
            await Factory.ExecuteSqlAsync($@"
                INSERT INTO gen_TenantLicencias (TenantId, TipoLicenciaId, Cantidad, StartDatetime, ExpireDatetime, ModifiedDatetime)
                VALUES ({TestData.ActiveTenantId}, 2, 5, '2024-01-01', '2024-01-02', '2024-01-01');");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(userName: "nuevo-venc", licencia: Payloads.License(tipoLicenciaId: 2)));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorLicenseExpired);
            var creado = await Factory.QueryScalarAsync<int>(
                "SELECT COUNT(*) FROM gen_Usuarios WHERE UserName = 'nuevo-venc'");
            Assert.Equal(0, creado);
        }

        [Fact] // USR-28 — admin de tenant con licencia NO activa -> 400 ErrorAdminLicensesNotAssaigned
        public async Task Editar_admin_con_licencia_inactiva_devuelve_400()
        {
            // adminb2 (Administrador=true) se edita a sí mismo pasando su licencia (Id=4) con IsActive=false.
            // El gate `dto.Administrador && !TipoLicenciaActiva.IsActive` corta: un admin no puede quedar sin
            // licencia activa. (Administrador se preserva desde la entidad en la edición, no viaja en el body.)
            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(id: TestData.AdminB2Id, userName: "adminb2", email: "adminb2@tech-bi.com",
                    licencia: Payloads.License(tipoLicenciaId: 1, id: 4, isActive: false)));

            await resp.ShouldBeBadRequest(MessagesAPI.ErrorAdminLicensesNotAssaigned);
            var sigueActiva = await Factory.QueryScalarAsync<int>(
                "SELECT IsActive FROM gen_UsuarioTipoLicencia WHERE Id = 4");
            Assert.Equal(1, sigueActiva); // sin efecto colateral
        }

        [Fact] // USR-29 — cambiar la licencia a inactiva desactiva la previa y bumpea el security stamp (re-login)
        public async Task Editar_a_licencia_inactiva_desactiva_la_previa_y_bumpea_stamp()
        {
            // userb (no-admin) tiene la licencia id=1 (TLI=1) activa. La editamos a IsActive=false.
            var stampAntes = await Factory.QueryScalarAsync<string>(
                $"SELECT SecurityStamp FROM gen_Usuarios WHERE Id = {TestData.UserBId}");

            using var client = await CreateAuthenticatedClientAsync(TestData.AdminB2);
            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(id: TestData.UserBId, userName: "userb", email: "userb@tech-bi.com",
                    licencia: Payloads.License(tipoLicenciaId: 1, id: 1, isActive: false)));

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var activa = await Factory.QueryScalarAsync<int>(
                "SELECT IsActive FROM gen_UsuarioTipoLicencia WHERE Id = 1");
            Assert.Equal(0, activa); // licencia previa desactivada
            var stampDespues = await Factory.QueryScalarAsync<string>(
                $"SELECT SecurityStamp FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.NotEqual(stampAntes, stampDespues); // stamp bumpeado => sesión viva invalidada
        }
    }
}
