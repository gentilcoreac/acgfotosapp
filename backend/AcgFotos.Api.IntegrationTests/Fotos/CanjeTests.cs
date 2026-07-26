using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Canje de código → token de sesión de familia (Fase 2, ADR-02/ADR-11). El endpoint es
    /// [AllowAnonymous]: los tests usan <see cref="IntegrationTestBase.CreateClient"/> (sin token)
    /// para el canje en sí, y un cliente autenticado de plataforma solo para el arrange (crear el
    /// evento/grupo/participante de prueba).
    /// </summary>
    public class CanjeTests : IntegrationTestBase
    {
        public CanjeTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<(EventoDto Evento, GrupoDto Grupo)> CrearEventoConParticipanteAsync(
            HttpClient client, EstadoEvento estado = EstadoEvento.Publicado, DateTime? fechaExpiracion = null, string participante = "Ana Pérez")
        {
            var eventoResp = await client.PostAsJsonAsync("/api/fotos/eventos/update", new
            {
                id = 0,
                nombre = "Egresados 2026",
                estado = (int)estado,
                fechaExpiracion,
                tamanosPrecios = Array.Empty<object>(),
            });
            await eventoResp.ShouldBeOk();
            var evento = (await eventoResp.Content.ReadFromJsonAsync<EventoDto>())!;

            var grupoResp = await client.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId = evento.Id,
                nombre = "7ºB",
                participantes = new[] { new { id = 0, nombre = participante } },
            });
            await grupoResp.ShouldBeOk();
            var grupo = (await grupoResp.Content.ReadFromJsonAsync<GrupoDto>())!;

            return (evento, grupo);
        }

        [Fact] // CAN-01 — código válido: 200, token de familia con los claims correctos y ~30 min de vigencia
        public async Task Codigo_valido_emite_token_de_sesion()
        {
            using var admin = await CreateTenantClientAsync(tenantId: 2);
            var (evento, grupo) = await CrearEventoConParticipanteAsync(admin);
            var participante = grupo.Participantes.Single();
            var codigo = participante.CodigoAcceso!;

            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeOk();

            var dto = (await resp.Content.ReadFromJsonAsync<CanjeResultDto>())!;
            Assert.Equal(evento.Id, dto.EventoId);
            Assert.Equal("Egresados 2026", dto.NombreEvento);
            Assert.Equal(new[] { participante.Id }, dto.Participantes.Select(p => p.Id));
            Assert.Equal("Ana Pérez", dto.Participantes.Single().Nombre);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.Token);
            Assert.Equal("familia", jwt.Claims.Single(c => c.Type == "sessionType").Value);
            Assert.Equal("2", jwt.Claims.Single(c => c.Type == "tenant").Value);
            Assert.Equal(evento.Id.ToString(), jwt.Claims.Single(c => c.Type == "eventoId").Value);
            Assert.Equal(
                new[] { participante.Id.ToString() },
                jwt.Claims.Where(c => c.Type == "participanteId").Select(c => c.Value));

            var minutosRestantes = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;
            Assert.InRange(minutosRestantes, 28, 31);
        }

        [Fact] // CAN-02 — código inexistente: 400 con mensaje genérico
        public async Task Codigo_inexistente_da_400()
        {
            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo = "ZZZZ-9999" });
            await resp.ShouldBeBadRequest("Código inválido o vencido.");
        }

        [Fact] // CAN-03 — código desactivado: 400 (mismo mensaje genérico, no revela que existió)
        public async Task Codigo_inactivo_da_400()
        {
            using var admin = await CreateTenantClientAsync(tenantId: 2);
            var (_, grupo) = await CrearEventoConParticipanteAsync(admin);
            var codigo = grupo.Participantes.Single().CodigoAcceso!;

            await Factory.ExecuteSqlAsync($"UPDATE fot_CodigosAcceso SET Activo = false WHERE Codigo = '{codigo}'");

            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeBadRequest("Código inválido o vencido.");
        }

        [Fact] // CAN-04 — evento en Borrador (todavía no publicado): 400
        public async Task Evento_en_borrador_da_400()
        {
            using var admin = await CreateTenantClientAsync(tenantId: 2);
            var (_, grupo) = await CrearEventoConParticipanteAsync(admin, estado: EstadoEvento.Borrador);
            var codigo = grupo.Participantes.Single().CodigoAcceso!;

            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeBadRequest("Código inválido o vencido.");
        }

        [Fact] // CAN-05 — evento Cerrado: 400
        public async Task Evento_cerrado_da_400()
        {
            using var admin = await CreateTenantClientAsync(tenantId: 2);
            var (_, grupo) = await CrearEventoConParticipanteAsync(admin, estado: EstadoEvento.Cerrado);
            var codigo = grupo.Participantes.Single().CodigoAcceso!;

            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeBadRequest("Código inválido o vencido.");
        }

        [Fact] // CAN-06 — evento con FechaExpiracion ya pasada: 400
        public async Task Evento_expirado_da_400()
        {
            using var admin = await CreateTenantClientAsync(tenantId: 2);
            var (_, grupo) = await CrearEventoConParticipanteAsync(
                admin, fechaExpiracion: DateTime.UtcNow.AddDays(-1));
            var codigo = grupo.Participantes.Single().CodigoAcceso!;

            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeBadRequest("Código inválido o vencido.");
        }

        [Fact] // CAN-07 — /canje excede CanjePolicy -> 429 (mismo patrón que AuthRateLimitTests)
        public async Task Canje_excede_el_rate_limit()
        {
            using var factory = Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:Canje:PermitLimit"] = "2",
                    ["RateLimiting:Global:PermitLimit"] = "1000000", // que no interfiera
                })));
            using var client = factory.CreateClient();

            HttpResponseMessage? last = null;
            for (var i = 0; i < 3; i++)
            {
                last = await client.PostAsJsonAsync("/api/fotos/canje", new { codigo = "ZZZZ-9999" });
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        }
    }
}
