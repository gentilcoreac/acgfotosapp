using System.Net;
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
    /// Admin de pedidos (Fase 2, <c>api/fotos/pedidos</c>): listado por evento/estado, detalle y
    /// cambio de estado. No hay create/update/delete acá (ADR-07: el pedido lo confirma la familia,
    /// ver <see cref="FamiliaPedidoTests"/>) — estos tests arman el pedido por ese camino real y
    /// después ejercitan el admin.
    /// </summary>
    public class PedidoAdminTests : IntegrationTestBase
    {
        public PedidoAdminTests(TestWebApplicationFactory factory) : base(factory) { }

        #region Helpers

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static async Task<EventoDto> CrearEventoConTamanoAsync(HttpClient client, string nombre = "Egresados 2026")
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update", new
            {
                id = 0,
                nombre,
                estado = (int)EstadoEvento.Publicado,
                tamanosPrecios = new[] { new { id = 0, nombre = "10x15", precioUnitario = 500m, orden = 0, activo = true } },
            });
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<EventoDto>())!;
        }

        private static async Task<ParticipanteDto> CrearParticipanteAsync(HttpClient client, long eventoId, string nombre = "Ana Pérez")
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/grupos/update", new
            {
                id = 0,
                eventoId,
                nombre = "7ºB",
                participantes = new[] { new { id = 0, nombre } },
            });
            await resp.ShouldBeOk();
            var grupo = (await resp.Content.ReadFromJsonAsync<GrupoDto>())!;
            return grupo.Participantes.Single();
        }

        private static byte[] CrearJpeg()
        {
            using var imagen = new Image<Rgb24>(800, 600, new Rgb24(40, 90, 160));
            using var ms = new MemoryStream();
            imagen.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private static async Task<long> SubirFotoAsync(HttpClient client, long grupoId, long participanteId)
        {
            var multipart = new MultipartFormDataContent
            {
                { new StringContent(grupoId.ToString()), "grupoId" },
                { new StringContent(participanteId.ToString()), "participanteId" },
            };
            var archivo = new ByteArrayContent(CrearJpeg());
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipart.Add(archivo, "archivos", "foto.jpg");
            var resp = await client.PostAsync("/api/fotos/fotos/upload", multipart);
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<List<FotoDto>>())!.Single().Id;
        }

        /// <summary>Espera a que el worker en background termine de procesar la foto (thumb+preview).</summary>
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

        /// <summary>Evento + participante + foto + un pedido confirmado por la familia (2 copias, $500 c/u).</summary>
        private async Task<(long EventoId, long PedidoId, string ParticipanteNombre)> ArmarPedidoAsync(
            HttpClient admin, string nombreEvento = "Egresados 2026")
        {
            var evento = await CrearEventoConTamanoAsync(admin, nombreEvento);
            var tamano = evento.TamanosPrecios.Single();
            var participante = await CrearParticipanteAsync(admin, evento.Id);

            var grupoResp = await admin.GetAsync($"/api/fotos/grupos?eventoId={evento.Id}");
            await grupoResp.ShouldBeOk();
            var grupoId = (await grupoResp.Content.ReadFromJsonAsync<Page<GrupoHeaderDto>>())!.Items.Single().Id;

            var fotoId = await SubirFotoAsync(admin, grupoId, participante.Id);
            await EsperarProcesamientoAsync(fotoId);

            using var familia = await CanjearAsync(participante.CodigoAcceso!);
            var pedidoResp = await familia.PostAsJsonAsync("/api/fotos/familia/pedidos", new
            {
                nombreContacto = "Familia Pérez",
                telefonoContacto = "+54 9 11 5555-5555",
                items = new[] { new { fotoId, tamanoPrecioId = tamano.Id, cantidad = 2 } },
            });
            await pedidoResp.ShouldBeOk();
            var pedidoId = (await pedidoResp.Content.ReadFromJsonAsync<PedidoConfirmadoDto>())!.Id;

            return (evento.Id, pedidoId, participante.Nombre);
        }

        #endregion

        [Fact] // PED-01 — listado filtra por evento, trae cantidadItems/total correctos
        public async Task Listado_filtra_por_evento_y_trae_totales()
        {
            using var admin = await CreateTenantClientAsync();
            var (eventoId, pedidoId, participanteNombre) = await ArmarPedidoAsync(admin);
            var (otroEventoId, _, _) = await ArmarPedidoAsync(admin, "Otro evento");

            var page = await (await admin.GetAsync($"/api/fotos/pedidos?eventoId={eventoId}"))
                .Content.ReadFromJsonAsync<Page<PedidoHeaderDto>>();

            var pedido = Assert.Single(page!.Items);
            Assert.Equal(pedidoId, pedido.Id);
            Assert.Equal(participanteNombre, pedido.ParticipanteNombre);
            Assert.Equal(1, pedido.CantidadItems);
            Assert.Equal(1000m, pedido.Total); // 2 copias × $500
            Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
            Assert.DoesNotContain(page.Items, p => p.Id != pedidoId);
            Assert.NotEqual(eventoId, otroEventoId);
        }

        [Fact] // PED-02 — listado filtra por estado
        public async Task Listado_filtra_por_estado()
        {
            using var admin = await CreateTenantClientAsync();
            var (eventoId, pedidoId, _) = await ArmarPedidoAsync(admin);

            var pendientes = await (await admin.GetAsync($"/api/fotos/pedidos?eventoId={eventoId}&estado={(int)EstadoPedido.Pendiente}"))
                .Content.ReadFromJsonAsync<Page<PedidoHeaderDto>>();
            Assert.Single(pendientes!.Items);

            var impresos = await (await admin.GetAsync($"/api/fotos/pedidos?eventoId={eventoId}&estado={(int)EstadoPedido.Impreso}"))
                .Content.ReadFromJsonAsync<Page<PedidoHeaderDto>>();
            Assert.Empty(impresos!.Items);
        }

        [Fact] // PED-03 — detalle trae las líneas con el nombre del tamaño
        public async Task Detalle_trae_items_con_nombre_de_tamano()
        {
            using var admin = await CreateTenantClientAsync();
            var (_, pedidoId, _) = await ArmarPedidoAsync(admin);

            var resp = await admin.GetAsync($"/api/fotos/pedidos/{pedidoId}");
            await resp.ShouldBeOk();
            var detalle = (await resp.Content.ReadFromJsonAsync<PedidoDetalleDto>())!;

            var item = Assert.Single(detalle.Items);
            Assert.Equal("10x15", item.TamanoPrecioNombre);
            Assert.Equal(2, item.Cantidad);
            Assert.Equal(500m, item.PrecioUnitarioSnapshot);
        }

        [Fact] // PED-04 — workflow válido: Pendiente → Impreso → Entregado
        public async Task Cambiar_estado_workflow_valido()
        {
            using var admin = await CreateTenantClientAsync();
            var (_, pedidoId, _) = await ArmarPedidoAsync(admin);

            var aImpreso = await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Impreso });
            await aImpreso.ShouldBeOk();
            Assert.Equal(EstadoPedido.Impreso, (EstadoPedido)await CountAsync($"SELECT Estado FROM fot_Pedidos WHERE Id = {pedidoId}"));

            var aEntregado = await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Entregado });
            await aEntregado.ShouldBeOk();
            Assert.Equal(EstadoPedido.Entregado, (EstadoPedido)await CountAsync($"SELECT Estado FROM fot_Pedidos WHERE Id = {pedidoId}"));
        }

        [Fact] // PED-05 — saltar directo de Pendiente a Entregado está permitido (corrección manual excepcional)
        public async Task Cambiar_estado_permite_saltar_pasos()
        {
            using var admin = await CreateTenantClientAsync();
            var (_, pedidoId, _) = await ArmarPedidoAsync(admin);

            var resp = await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Entregado });

            await resp.ShouldBeOk();
            Assert.Equal(EstadoPedido.Entregado, (EstadoPedido)await CountAsync($"SELECT Estado FROM fot_Pedidos WHERE Id = {pedidoId}"));
        }

        [Fact] // PED-06 — se puede retroceder un estado ya avanzado (por si se tocó sin querer)
        public async Task Cambiar_estado_permite_retroceder()
        {
            using var admin = await CreateTenantClientAsync();
            var (_, pedidoId, _) = await ArmarPedidoAsync(admin);
            await (await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Impreso }))
                .ShouldBeOk();
            await (await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Entregado }))
                .ShouldBeOk();

            var resp = await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Impreso });

            await resp.ShouldBeOk();
            Assert.Equal(EstadoPedido.Impreso, (EstadoPedido)await CountAsync($"SELECT Estado FROM fot_Pedidos WHERE Id = {pedidoId}"));
        }

        [Fact] // PED-07 — el único caso rechazado: "cambiar" al mismo estado en el que ya está
        public async Task Cambiar_estado_rechaza_no_op()
        {
            using var admin = await CreateTenantClientAsync();
            var (_, pedidoId, _) = await ArmarPedidoAsync(admin);

            var resp = await admin.PostAsJsonAsync($"/api/fotos/pedidos/{pedidoId}/estado", new { estado = (int)EstadoPedido.Pendiente });

            await resp.ShouldBeBadRequest();
        }

        [Fact] // PED-08 — aislamiento multi-tenant: el pedido de tenant 2 no existe para tenant 3
        public async Task Pedido_de_otro_tenant_es_invisible()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var (eventoId, pedidoId, _) = await ArmarPedidoAsync(tenant2);

            using var tenant3 = await CreateTenantClientAsync(3);

            await (await tenant3.GetAsync($"/api/fotos/pedidos/{pedidoId}")).ShouldBeStatus(HttpStatusCode.NotFound);

            var page = await (await tenant3.GetAsync($"/api/fotos/pedidos?eventoId={eventoId}"))
                .Content.ReadFromJsonAsync<Page<PedidoHeaderDto>>();
            Assert.Empty(page!.Items);
        }
    }
}
