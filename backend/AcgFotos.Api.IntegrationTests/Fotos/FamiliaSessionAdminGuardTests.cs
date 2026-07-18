using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Defensa en profundidad (2026-07-18): con <c>AuthorizationEnabled=false</c> (default de la app,
    /// dev y prod hoy — ver docs/05-notas-abiertas.md), el allowlist <c>[AllowFamiliaSession]</c> de
    /// <c>EndpointAuthoritation</c> NUNCA corre, así que sin esta defensa un JWT de familia recién
    /// canjeado podía llamar cualquier endpoint admin del vertical. Reproduce EXACTAMENTE lo que un
    /// smoke test manual confirmó como explotable (una sesión de familia borrando la foto de OTRO
    /// participante) y prueba que ahora los AppServices admin (Evento/Grupo/Foto) rechazan la sesión
    /// de familia por su cuenta, con 403 — corre a propósito en el host BASE (authz off), no en
    /// <c>AuthzWebApplicationFactory</c>: el punto es que el guard no dependa de ese flag.
    /// </summary>
    public class FamiliaSessionAdminGuardTests : IntegrationTestBase
    {
        public FamiliaSessionAdminGuardTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        /// <summary>Evento Publicado + grupo + participante; devuelve también el token de familia canjeado.</summary>
        private async Task<(long EventoId, long GrupoId, ParticipanteDto Participante, HttpClient Familia)> ArmarSesionAsync()
        {
            using var admin = await CreateTenantClientAsync();

            var eventoResp = await admin.PostAsJsonAsync("/api/fotos/eventos/update", new
            {
                id = 0,
                nombre = "Guard Test",
                estado = (int)EstadoEvento.Publicado,
                tamanosPrecios = Array.Empty<object>(),
            });
            await eventoResp.ShouldBeOk();
            var eventoId = (await eventoResp.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var grupoResp = await admin.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºB",
                participantes = new[] { new { id = 0, nombre = "Ana Pérez" } },
            });
            await grupoResp.ShouldBeOk();
            var grupo = (await grupoResp.Content.ReadFromJsonAsync<GrupoDto>())!;
            var participante = grupo.Participantes.Single();

            using var anonimo = CreateClient();
            var canjeResp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo = participante.CodigoAcceso });
            await canjeResp.ShouldBeOk();
            var dto = (await canjeResp.Content.ReadFromJsonAsync<CanjeResultDto>())!;

            var familia = CreateClient();
            familia.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.Token);
            return (eventoId, grupo.Id, participante, familia);
        }

        [Fact] // FAMGUARD-01 — reproduce el hallazgo del smoke test: ya NO puede borrar una foto por el endpoint admin
        public async Task Familia_no_puede_borrar_foto_por_el_endpoint_admin()
        {
            var (_, grupoId, participante, familia) = await ArmarSesionAsync();

            using var admin = await CreateTenantClientAsync();
            var multipart = new MultipartFormDataContent { { new StringContent(grupoId.ToString()), "grupoId" } };
            var archivo = new ByteArrayContent("no es una imagen real, no hace falta para este test"u8.ToArray());
            multipart.Add(archivo, "archivos", "x.jpg");
            var subida = await admin.PostAsync("/api/fotos/fotos/upload", multipart);
            await subida.ShouldBeOk();
            var fotoId = (await subida.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single().Id;

            var resp = await familia.DeleteAsync($"/api/fotos/fotos/{fotoId}");

            await resp.ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Fotos WHERE Id = {fotoId}"));
        }

        [Fact] // FAMGUARD-02 — tampoco puede subir, listar (admin) ni pedir thumb/preview/original por ahí
        public async Task Familia_no_puede_usar_los_demas_endpoints_admin_de_fotos()
        {
            var (_, grupoId, _, familia) = await ArmarSesionAsync();

            var upload = new MultipartFormDataContent { { new StringContent(grupoId.ToString()), "grupoId" } };
            upload.Add(new ByteArrayContent("x"u8.ToArray()), "archivos", "x.jpg");
            await (await familia.PostAsync("/api/fotos/fotos/upload", upload))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);

            await (await familia.GetAsync($"/api/fotos/fotos?grupoId={grupoId}"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);

            await (await familia.GetAsync("/api/fotos/fotos/1/thumb"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);

            await (await familia.GetAsync("/api/fotos/fotos/1/preview"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);

            await (await familia.GetAsync("/api/fotos/fotos/1/original"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
        }

        [Fact] // FAMGUARD-03 — ni el CRUD de eventos ni el de grupos (incluidas las tarjetas con los códigos QR)
        public async Task Familia_no_puede_usar_el_crud_de_eventos_ni_grupos()
        {
            var (eventoId, grupoId, _, familia) = await ArmarSesionAsync();

            await (await familia.GetAsync("/api/fotos/eventos"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
            await (await familia.GetAsync($"/api/fotos/eventos/{eventoId}"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
            await (await familia.DeleteAsync($"/api/fotos/eventos/{eventoId}"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);

            await (await familia.GetAsync("/api/fotos/grupos"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
            await (await familia.GetAsync($"/api/fotos/grupos/{grupoId}"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
            // Las tarjetas exponen el código de acceso + QR de TODOS los participantes del grupo: fuga grave.
            await (await familia.GetAsync($"/api/fotos/grupos/{grupoId}/tarjetas"))
                .ShouldBeError(HttpStatusCode.Forbidden, MessagesAPI.ErrorUserNotPrivilegesAccess);
        }

        [Fact] // FAMGUARD-04 — control: la sesión de familia SIGUE pudiendo usar su propia galería
        public async Task Familia_sigue_pudiendo_usar_su_propia_galeria()
        {
            var (_, _, _, familia) = await ArmarSesionAsync();

            await (await familia.GetAsync("/api/fotos/familia/fotos")).ShouldBeOk();
        }
    }
}
