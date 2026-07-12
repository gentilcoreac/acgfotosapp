using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Usuarios
{
    /// <summary>
    /// Perfil propio: mi-perfil (USR-59/60/61) y update-profile (USR-65/100). Ambos son self-service y
    /// SIEMPRE operan sobre el usuario del contexto, no sobre un Id del body:
    ///  - mi-perfil no recibe id -> no hay forma de pedir el perfil ajeno (USR-59).
    ///  - update-profile ahora resuelve el usuario por AppContext.UserId, no por dto.Id -> cierra el IDOR
    ///    de editar el perfil de otro usuario del mismo tenant (USR-65, hallazgo #15) y no deja escalar
    ///    Administrador/EmailConfirmed aunque el DTO los exponga (USR-100).
    /// </summary>
    public class UsuarioPerfilTests : IntegrationTestBase
    {
        public UsuarioPerfilTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record Perfil(string UserName, string Nombre);

        [Fact] // USR-59 — mi-perfil devuelve SIEMPRE el perfil del contexto (no recibe id)
        public async Task MiPerfil_usa_el_contexto()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var perfil = await (await client.GetAsync("/api/general/usuarios/mi-perfil"))
                .Content.ReadFromJsonAsync<Perfil>();

            Assert.Equal("userb", perfil!.UserName); // el del token, no hay parámetro para pedir otro
        }

        [Fact] // USR-60/61 — mi-perfil sin contexto de usuario válido -> error de cliente controlado (no 500)
        public async Task MiPerfil_sin_token_no_filtra_y_no_revienta()
        {
            using var client = CreateClient(); // sin Authorization

            var resp = await client.GetAsync("/api/general/usuarios/mi-perfil");

            // El host base no fuerza [Authorize]; el contexto anónimo (UserName null) cae en
            // ErrorUserRecuparate (400) — nunca un 200 con datos ni un 500 opaco.
            Assert.True(resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest,
                $"esperaba 401/400 pero fue {(int)resp.StatusCode}");
        }

        [Fact] // USR-65 — update-profile NO puede editar a otro usuario vía dto.Id (IDOR cerrado)
        public async Task UpdateProfile_ignora_dto_Id_y_edita_al_del_contexto()
        {
            // userb intenta editar el perfil de adminb2 (Id=15) pasándolo en dto.Id.
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update-profile", new
            {
                id = TestData.AdminB2Id,        // víctima: NO debe tocarse
                userName = "userb",
                nombre = "PWNED",
                apellido = "B",
                email = "userb@tech-bi.com",
                telefono = 1122334455L,
            });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var adminb2 = await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Usuarios WHERE Id = {TestData.AdminB2Id}");
            Assert.Equal("Admin", adminb2); // adminb2 intacto: dto.Id ignorado
            var userb = await Factory.QueryScalarAsync<string>($"SELECT Nombre FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.Equal("PWNED", userb);   // el caller editó SU propio perfil (contexto)
        }

        [Fact] // USR-100 — update-profile no permite escalar Administrador aunque el DTO lo exponga
        public async Task UpdateProfile_no_escala_administrador()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // no-admin

            var resp = await client.PostAsJsonAsync("/api/general/usuarios/update-profile", new
            {
                id = TestData.UserBId,
                userName = "userb",
                nombre = "User",
                apellido = "B",
                email = "userb@tech-bi.com",
                telefono = 1122334455L,
                administrador = true,    // intento de escalada por el body
                emailConfirmed = true,
            });

            await resp.ShouldBeStatus(HttpStatusCode.OK);
            var esAdmin = await Factory.QueryScalarAsync<int>($"SELECT Administrador FROM gen_Usuarios WHERE Id = {TestData.UserBId}");
            Assert.Equal(0, esAdmin); // UpdateProfileAsync no asigna Administrador
        }
    }
}
