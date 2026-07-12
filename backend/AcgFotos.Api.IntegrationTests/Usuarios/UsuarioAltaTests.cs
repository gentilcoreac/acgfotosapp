using System;
using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Alta de usuario happy path (USR-01): write-path ADR-0001 vía SecurityManager.Register. El nuevo
    /// usuario nace con DateCreated=hoy (alimenta el reporte de altas) y SIN bloqueo (LockoutEnd null,
    /// aunque LockoutEnabled lo deje "bloqueable"). El alta sin licencia va por la rama sin cupos.
    /// </summary>
    public class UsuarioAltaTests : IntegrationTestBase
    {
        public UsuarioAltaTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record CreatedUser(long Id);

        [Fact] // USR-01 — alta happy path: 200, Id>0, DateCreated hoy, no bloqueado
        public async Task Alta_happy_path_crea_usuario()
        {
            using var client = await CreateAuthenticatedClientAsync(); // root en tenant raíz

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update",
                Payloads.User(userName: "nuevo-ok", nombre: "Nuevo", apellido: "OK"));

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var dto = await resp.Content.ReadFromJsonAsync<CreatedUser>();
            Assert.True(dto!.Id > 0); // entidad creada

            var row = await Factory.QueryScalarAsync<string>(@"
                SELECT CONCAT(TenantId, '|',
                              CASE WHEN CAST(DateCreated AS date) = CAST(GETDATE() AS date) THEN 1 ELSE 0 END, '|',
                              CASE WHEN LockoutEnd IS NULL THEN 1 ELSE 0 END)
                FROM gen_Usuarios WHERE UserName = 'nuevo-ok'");
            Assert.Equal($"{TestData.RootTenantId}|1|1", row); // tenant del contexto, DateCreated hoy, sin bloqueo
        }
    }
}
