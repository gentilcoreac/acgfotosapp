using System.Net.Http.Headers;
using System.Net.Http.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using AcgFotos.Fotos.Application.Dtos;
using AcgFotos.Fotos.Domain.Entities;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Carrito/pedido de la sesión de familia (ADR-11, Fase 2, <c>api/fotos/familia/tamanos-precios</c>
    /// + <c>api/fotos/familia/pedidos</c>): mismo criterio de alcance que <see cref="FamiliaGaleriaTests"/>
    /// — token real emitido por el canje, nunca un parámetro del request.
    /// </summary>
    public class FamiliaPedidoTests : IntegrationTestBase
    {
        public FamiliaPedidoTests(TestWebApplicationFactory factory) : base(factory) { }

        #region Helpers

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<EventoDto> CrearEventoConTamanosAsync(HttpClient client, params (string Nombre, decimal Precio, bool Activo)[] tamanos)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update", new
            {
                id = 0,
                nombre = "Egresados 2026",
                estado = (int)EstadoEvento.Publicado,
                tamanosPrecios = tamanos.Select((t, i) => new
                {
                    id = 0,
                    nombre = t.Nombre,
                    precioUnitario = t.Precio,
                    orden = i,
                    activo = t.Activo,
                }).ToArray(),
            });
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<EventoDto>())!;
        }

        private static async Task<(long GrupoId, ParticipanteDto Participante)> CrearGrupoConParticipanteAsync(
            HttpClient client, long eventoId)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºB",
                participantes = new[] { new { id = 0, nombre = "Ana Pérez" } },
            });
            await resp.ShouldBeOk();
            var grupo = (await resp.Content.ReadFromJsonAsync<GrupoDto>())!;
            return (grupo.Id, grupo.Participantes.Single());
        }

        private static byte[] CrearJpeg()
        {
            using var imagen = new Image<Rgb24>(800, 600, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private static async Task<FotoDto> SubirAsync(HttpClient client, long grupoId, long? participanteId, string nombre = "foto.jpg")
        {
            var multipart = new MultipartFormDataContent
            {
                { new StringContent(grupoId.ToString()), "grupoId" },
            };
            if (participanteId is not null)
            {
                multipart.Add(new StringContent(participanteId.Value.ToString()), "participanteId");
            }
            var archivo = new ByteArrayContent(CrearJpeg());
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipart.Add(archivo, "archivos", nombre);

            var resp = await client.PostAsync("/api/fotos/fotos/upload", multipart);
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single();
        }

        private async Task EsperarProcesamientoAsync(long fotoId)
        {
            for (var intento = 0; intento < 120; intento++) // hasta 30 s
            {
                var estado = (EstadoProcesamientoFoto)await CountAsync(
                    $"SELECT EstadoProcesamiento FROM fot_Fotos WHERE Id = {fotoId}");
                if (estado != EstadoProcesamientoFoto.Pendiente)
                {
                    return;
                }

                await Task.Delay(250);
            }

            Assert.Fail($"La foto {fotoId} sigue Pendiente tras 30 s: el worker no la procesó.");
        }

        /// <summary>Canjea el código (endpoint anónimo real) y arma un cliente con el JWT de familia.</summary>
        private async Task<HttpClient> CanjearAsync(string codigo)
        {
            using var anonimo = CreateClient();
            var resp = await anonimo.PostAsJsonAsync("/api/fotos/canje", new { codigo });
            await resp.ShouldBeOk();
            var dto = (await resp.Content.ReadFromJsonAsync<CanjeResultDto>())!;

            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.Token);
            return client;
        }

        /// <summary>Evento publicado con dos tamaños (uno inactivo), grupo, participante y su foto ya procesada.</summary>
        private async Task<(HttpClient Familia, FotoDto Foto, TamanoPrecioDto Activo, TamanoPrecioDto Inactivo)> ArmarEscenarioAsync(
            HttpClient admin)
        {
            var evento = await CrearEventoConTamanosAsync(
                admin,
                ("10x15", 500m, true),
                ("20x30", 1200m, false));
            var activo = evento.TamanosPrecios.Single(t => t.Activo);
            var inactivo = evento.TamanosPrecios.Single(t => !t.Activo);

            var (grupoId, ana) = await CrearGrupoConParticipanteAsync(admin, evento.Id);
            var foto = await SubirAsync(admin, grupoId, ana.Id);
            await EsperarProcesamientoAsync(foto.Id);

            var familia = await CanjearAsync(ana.CodigoAcceso!);
            return (familia, foto, activo, inactivo);
        }

        #endregion

        [Fact] // FAMPED-01 — el catálogo solo trae tamaños Activo=true, ordenados
        public async Task Catalogo_devuelve_solo_activos_ordenados()
        {
            using var admin = await CreateTenantClientAsync();
            var (familia, _, activo, inactivo) = await ArmarEscenarioAsync(admin);
            using var clienteFamilia = familia;

            var resp = await familia.GetAsync("/api/fotos/familia/tamanos-precios");
            await resp.ShouldBeOk();
            var tamanos = (await resp.Content.ReadFromJsonAsync<List<TamanoPrecioDto>>())!;

            Assert.Single(tamanos);
            Assert.Equal(activo.Id, tamanos[0].Id);
            Assert.DoesNotContain(tamanos, t => t.Id == inactivo.Id);
        }

        [Fact] // FAMPED-02 — confirmar con datos válidos crea el pedido con el precio del catálogo (no uno inventado)
        public async Task Confirmar_pedido_valido_crea_pedido_con_snapshot_de_precio()
        {
            using var admin = await CreateTenantClientAsync();
            var (familia, foto, activo, _) = await ArmarEscenarioAsync(admin);
            using var clienteFamilia = familia;

            var resp = await familia.PostAsJsonAsync("/api/fotos/familia/pedidos", new
            {
                nombreContacto = "Familia Pérez",
                telefonoContacto = "+54 9 11 5555-5555",
                items = new[] { new { fotoId = foto.Id, tamanoPrecioId = activo.Id, cantidad = 3 } },
            });
            await resp.ShouldBeOk();
            var confirmado = (await resp.Content.ReadFromJsonAsync<PedidoConfirmadoDto>())!;

            Assert.Equal(EstadoPedido.Pendiente, confirmado.Estado);
            Assert.Equal(3 * activo.PrecioUnitario, confirmado.Total);
            var item = Assert.Single(confirmado.Items);
            Assert.Equal(activo.PrecioUnitario, item.PrecioUnitarioSnapshot);

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Pedidos WHERE Id = {confirmado.Id}"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_PedidoItems WHERE PedidoId = {confirmado.Id}"));
        }

        [Fact] // FAMPED-03 — una foto que no pertenece a la sesión: rechazado
        public async Task Confirmar_con_foto_ajena_es_rechazado()
        {
            using var admin = await CreateTenantClientAsync();
            var evento = await CrearEventoConTamanosAsync(admin, ("10x15", 500m, true));
            var activo = evento.TamanosPrecios.Single();

            var (grupoId, ana) = await CrearGrupoConParticipanteAsync(admin, evento.Id);
            await SubirAsync(admin, grupoId, ana.Id); // foto de Ana, no se usa

            var grupoResp = await admin.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId = evento.Id,
                nombre = "7ºC",
                participantes = new[] { new { id = 0, nombre = "Beto Ruiz" } },
            });
            await grupoResp.ShouldBeOk();
            var grupoBeto = (await grupoResp.Content.ReadFromJsonAsync<GrupoDto>())!;
            var beto = grupoBeto.Participantes.Single();
            var fotoDeBeto = await SubirAsync(admin, grupoBeto.Id, beto.Id);
            await EsperarProcesamientoAsync(fotoDeBeto.Id);

            using var familia = await CanjearAsync(ana.CodigoAcceso!);

            var resp = await familia.PostAsJsonAsync("/api/fotos/familia/pedidos", new
            {
                nombreContacto = "Familia Pérez",
                telefonoContacto = "+54 9 11 5555-5555",
                items = new[] { new { fotoId = fotoDeBeto.Id, tamanoPrecioId = activo.Id, cantidad = 1 } },
            });
            await resp.ShouldBeBadRequest();
        }

        [Fact] // FAMPED-04 — un tamaño inactivo: rechazado
        public async Task Confirmar_con_tamano_inactivo_es_rechazado()
        {
            using var admin = await CreateTenantClientAsync();
            var (familia, foto, _, inactivo) = await ArmarEscenarioAsync(admin);
            using var clienteFamilia = familia;

            var resp = await familia.PostAsJsonAsync("/api/fotos/familia/pedidos", new
            {
                nombreContacto = "Familia Pérez",
                telefonoContacto = "+54 9 11 5555-5555",
                items = new[] { new { fotoId = foto.Id, tamanoPrecioId = inactivo.Id, cantidad = 1 } },
            });
            await resp.ShouldBeBadRequest();
        }

        [Fact] // FAMPED-05 — cantidad <= 0: rechazado
        public async Task Confirmar_con_cantidad_invalida_es_rechazado()
        {
            using var admin = await CreateTenantClientAsync();
            var (familia, foto, activo, _) = await ArmarEscenarioAsync(admin);
            using var clienteFamilia = familia;

            var resp = await familia.PostAsJsonAsync("/api/fotos/familia/pedidos", new
            {
                nombreContacto = "Familia Pérez",
                telefonoContacto = "+54 9 11 5555-5555",
                items = new[] { new { fotoId = foto.Id, tamanoPrecioId = activo.Id, cantidad = 0 } },
            });
            await resp.ShouldBeBadRequest();
        }

        [Fact] // FAMPED-06 — sin items: rechazado
        public async Task Confirmar_sin_items_es_rechazado()
        {
            using var admin = await CreateTenantClientAsync();
            var (familia, _, _, _) = await ArmarEscenarioAsync(admin);
            using var clienteFamilia = familia;

            var resp = await familia.PostAsJsonAsync("/api/fotos/familia/pedidos", new
            {
                nombreContacto = "Familia Pérez",
                telefonoContacto = "+54 9 11 5555-5555",
                items = Array.Empty<object>(),
            });
            await resp.ShouldBeBadRequest();
        }
    }
}
