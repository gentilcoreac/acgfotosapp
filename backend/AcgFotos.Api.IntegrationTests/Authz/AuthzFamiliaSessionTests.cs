using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Authz
{
    /// <summary>
    /// Allowlist de sesión de familia en <c>EndpointAuthoritation</c> (ADR-11): con
    /// <c>AuthorizationEnabled=true</c>, un JWT de familia solo puede pisar endpoints marcados
    /// <c>[AllowFamiliaSession]</c> — la matriz de permisos (gen_Usuarios/roles) no tiene fila para esa
    /// sesión, así que TODO lo demás es 403 aunque el token sea válido. Es la contraparte de seguridad
    /// de <see cref="Fotos.FamiliaGaleriaTests"/> (que corre con authz off y prueba el alcance de datos).
    /// </summary>
    public class AuthzFamiliaSessionTests : AuthzTestBase
    {
        public AuthzFamiliaSessionTests(AuthzWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        /// <summary>Evento Publicado + grupo + un participante; canjea su código y arma el cliente de familia.</summary>
        private async Task<HttpClient> CrearSesionDeFamiliaAsync()
        {
            using var admin = await CreateTenantClientAsync();

            var eventoResp = await admin.PostAsJsonAsync("/api/fotos/eventos/update", new
            {
                id = 0,
                nombre = "Egresados 2026",
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
            var codigo = grupo.Participantes.Single().CodigoAcceso!;

            using var anonimo = CreateClient();
            var canjeResp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await canjeResp.ShouldBeOk();
            var dto = (await canjeResp.Content.ReadFromJsonAsync<CanjeResultDto>())!;

            var familia = CreateClient();
            familia.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.Token);
            return familia;
        }

        [Fact] // AUTHZ-FAM-01 — endpoint [AllowFamiliaSession]: 200 aunque no haya ningún grant en la matriz
        public async Task Familia_accede_a_endpoint_marcado_sin_grant()
        {
            using var familia = await CrearSesionDeFamiliaAsync();

            var resp = await familia.GetAsync("/api/fotos/familia/fotos");

            await resp.ShouldBeStatus(HttpStatusCode.OK);
        }

        [Fact] // AUTHZ-FAM-02 — endpoint admin SIN la marca: 403, aunque el JWT de familia sea válido
        public async Task Familia_no_accede_a_endpoint_admin_sin_marca()
        {
            using var familia = await CrearSesionDeFamiliaAsync();

            var resp = await familia.GetAsync("/api/general/usuarios/mi-perfil");

            await resp.ShouldBeStatus(HttpStatusCode.Forbidden);
        }

        [Fact] // AUTHZ-FAM-03 — ni siquiera un endpoint del propio vertical Fotos sin la marca (admin, mismo módulo)
        public async Task Familia_no_accede_a_endpoint_admin_de_fotos_sin_marca()
        {
            using var familia = await CrearSesionDeFamiliaAsync();

            var resp = await familia.GetAsync("/api/fotos/fotos?grupoId=1");

            await resp.ShouldBeStatus(HttpStatusCode.Forbidden);
        }
    }
}
