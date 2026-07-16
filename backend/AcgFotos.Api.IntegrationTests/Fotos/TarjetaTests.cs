using System.Net;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Tarjetas de acceso por grupo (Fase 1: para imprimir y repartir a las familias): una por
    /// participante con su código activo, la URL de canje (molde <c>Fotos:UrlCanjeTemplate</c>) y el QR
    /// como PNG base64 listo para renderizar.
    /// </summary>
    public class TarjetaTests : IntegrationTestBase
    {
        public TarjetaTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<GrupoDto> CrearGrupoConParticipantesAsync(HttpClient client, params string[] participantes)
        {
            var evento = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                new { id = 0, nombre = "Egresados 2026", estado = 0, tamanosPrecios = Array.Empty<object>() });
            await evento.ShouldBeOk();
            var eventoId = (await evento.Content.ReadFromJsonAsync<EventoDto>())!.Id;

            var grupo = await client.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºB",
                participantes = participantes.Select(a => new { id = 0, nombre = a }).ToArray(),
            });
            await grupo.ShouldBeOk();
            return (await grupo.Content.ReadFromJsonAsync<GrupoDto>())!;
        }

        [Fact] // TAR-01 — una tarjeta por participante: código del participante, URL de canje y QR PNG válido
        public async Task Tarjetas_traen_codigo_url_y_qr_por_participante()
        {
            using var client = await CreateTenantClientAsync();
            var grupo = await CrearGrupoConParticipantesAsync(client, "Zoe Vega", "Ana Pérez");

            var resp = await client.GetAsync($"/api/fotos/grupos/{grupo.Id}/tarjetas");
            await resp.ShouldBeOk();

            var dto = (await resp.Content.ReadFromJsonAsync<TarjetasGrupoDto>())!;
            Assert.Equal(grupo.Id, dto.GrupoId);
            Assert.Equal("7ºB", dto.NombreGrupo);
            Assert.Equal("Egresados 2026", dto.NombreEvento);
            Assert.Equal(new[] { "Ana Pérez", "Zoe Vega" }, dto.Tarjetas.Select(t => t.Nombre));

            foreach (var tarjeta in dto.Tarjetas)
            {
                // El código de la tarjeta es EL del participante (mismo que devuelve el detalle del grupo).
                var participante = grupo.Participantes.Single(a => a.Id == tarjeta.ParticipanteId);
                Assert.Equal(participante.CodigoAcceso, tarjeta.Codigo);

                Assert.Contains(tarjeta.Codigo!, tarjeta.UrlCanje);

                // El QR es un PNG de verdad (magic bytes ‰PNG).
                var png = Convert.FromBase64String(tarjeta.QrPngBase64!);
                Assert.True(png.Length > 4 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47,
                    "el QR no es un PNG");
            }
        }

        [Fact] // TAR-02 — grupo inexistente o de otro tenant → 404
        public async Task Grupo_inexistente_o_ajeno_da_404()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            await (await tenant2.GetAsync("/api/fotos/grupos/999999/tarjetas"))
                .ShouldBeStatus(HttpStatusCode.NotFound);

            var grupoDeTenant2 = await CrearGrupoConParticipantesAsync(tenant2, "Ana");

            using var tenant3 = await CreateTenantClientAsync(3);
            await (await tenant3.GetAsync($"/api/fotos/grupos/{grupoDeTenant2.Id}/tarjetas"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }
    }
}
