using System.Net;
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
    /// Eventos — raíz del vertical Fotos. CRUD Extended con el catálogo de tamaños como colección
    /// hija reconciliada por Id en SyncCollections (Id 0 = alta, ausente = baja, presente = update
    /// de campos). Se opera como root + <c>SimulatedTenant:2</c> (root en su propio tenant NO
    /// persiste entidades multi-tenant — guard AllowSaveMultiTenantEntities, mismo esquema que
    /// usaban los tests de Budget). Aislamiento por el filtro global de tenant del DbContext.
    /// </summary>
    public class EventoCrudTests : IntegrationTestBase
    {
        public EventoCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static object Input(string nombre, long id = 0, string? lugarOrganizacion = null,
            EstadoEvento estado = EstadoEvento.Borrador, object[]? tamanos = null,
            long? perfilMarcaAguaId = null, long? opcionesPublicacionId = null) => new
        {
            id,
            nombre,
            lugarOrganizacion,
            estado = (int)estado,
            perfilMarcaAguaId,
            opcionesPublicacionId,
            tamanosPrecios = tamanos ?? Array.Empty<object>(),
        };

        /// <summary>Perfil de marca de agua real del tenant del cliente (el alta de la primera capa
        /// crea el perfil, design.md D14 — no hay endpoint de "crear perfil vacío").</summary>
        private static async Task<long> CreatePerfilMarcaAguaAsync(HttpClient client)
        {
            using var img = new Image<Rgba32>(80, 60, new Rgba32(255, 255, 255, 128));
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            using var multipart = new MultipartFormDataContent
            {
                { new StringContent("Perfil de prueba"), "nombrePerfilSiNuevo" },
                { new ByteArrayContent(ms.ToArray()), "archivo", "capa.png" },
            };
            var resp = await client.PostAsync("/api/fotos/marca-agua/perfiles/capas/upload", multipart);
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<CapaMarcaAguaSubidaDto>())!.Perfil.Id;
        }

        /// <summary>Opciones de publicación reales del tenant del cliente.</summary>
        private static async Task<long> CreateOpcionesPublicacionAsync(HttpClient client)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/marca-agua/opciones-publicacion/update", new
            {
                id = 0L,
                nombre = "Opciones de prueba",
                esDefault = false,
                ladoMayorPreview = 1600,
                ladoMayorThumb = 600,
                calidad = 80,
            });
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<OpcionesPublicacionDto>())!.Id;
        }

        private static object Tamano(string nombre, decimal precio, long id = 0, int orden = 0, bool activo = true) =>
            new { id, nombre, precioUnitario = precio, orden, activo };

        private async Task<long> CreateEventoAsync(HttpClient client, string nombre, object[]? tamanos = null,
            string? lugarOrganizacion = null)
        {
            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                Input(nombre, lugarOrganizacion: lugarOrganizacion, tamanos: tamanos));
            await resp.ShouldBeOk();
            var dto = await resp.Content.ReadFromJsonAsync<EventoDto>();
            return dto!.Id;
        }

        [Fact] // EVT-01 — alta happy: persiste escalares + catálogo, y estampa el tenant del contexto
        public async Task Alta_persiste_evento_con_catalogo_y_tenant()
        {
            using var client = await CreateTenantClientAsync(); // tenant 2

            var id = await CreateEventoAsync(client, "Egresados 2026", lugarOrganizacion: "San José",
                tamanos: new[] { Tamano("10x15", 1500), Tamano("13x18", 2500, orden: 1) });

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_Eventos WHERE Id = {id} AND TenantId = 2"));
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM fot_TamanosPrecios WHERE EventoId = {id} AND TenantId = 2"));
        }

        [Fact] // EVT-02 — detalle: trae el catálogo; el DTO no expone TenantId
        public async Task Detalle_trae_catalogo()
        {
            using var client = await CreateTenantClientAsync();
            var id = await CreateEventoAsync(client, "Con catálogo",
                tamanos: new[] { Tamano("20x30", 5000) });

            var resp = await client.GetAsync($"/api/fotos/eventos/{id}");
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<EventoDto>();
            Assert.Equal("Con catálogo", dto!.Nombre);
            var tamano = Assert.Single(dto.TamanosPrecios);
            Assert.Equal("20x30", tamano.Nombre);
            Assert.Equal(5000, tamano.PrecioUnitario);
        }

        [Fact] // EVT-03 — detalle id inexistente → 404
        public async Task Detalle_id_inexistente_da_404()
        {
            using var client = await CreateTenantClientAsync();

            var resp = await client.GetAsync("/api/fotos/eventos/999999");

            await resp.ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // EVT-04 — listado: searchText filtra por nombre o lugarOrganizacion
        public async Task Listado_filtra_por_nombre_o_lugarOrganizacion()
        {
            using var client = await CreateTenantClientAsync();
            await CreateEventoAsync(client, "Egresados Primaria", lugarOrganizacion: "San Martín");
            await CreateEventoAsync(client, "Jardín 2026", lugarOrganizacion: "Belgrano");

            var resp = await client.GetAsync("/api/fotos/eventos?searchText=Martín");
            await resp.ShouldBeOk();

            var page = await resp.Content.ReadFromJsonAsync<Page<EventoHeaderDto>>();
            var item = Assert.Single(page!.Items);
            Assert.Equal("Egresados Primaria", item.Nombre);
        }

        [Fact] // EVT-05 — edición: escalares + reconciliación del catálogo (update, baja y alta de filas)
        public async Task Edicion_reconcilia_catalogo_por_id()
        {
            using var client = await CreateTenantClientAsync();
            var id = await CreateEventoAsync(client, "Original",
                tamanos: new[] { Tamano("10x15", 1500), Tamano("13x18", 2500) });

            var detalle = await (await client.GetAsync($"/api/fotos/eventos/{id}"))
                .Content.ReadFromJsonAsync<EventoDto>();
            var chico = detalle!.TamanosPrecios.Single(t => t.Nombre == "10x15");

            // Se edita el 10x15 (sube el precio), se elimina el 13x18 y se agrega el 20x30.
            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                Input("Renombrado", id: id, estado: EstadoEvento.Publicado, tamanos: new[]
                {
                    Tamano("10x15", 1800, id: chico.Id),
                    Tamano("20x30", 5000),
                }));
            await resp.ShouldBeOk();

            Assert.Equal("Renombrado", await Factory.QueryScalarAsync<string>(
                $"SELECT Nombre FROM fot_Eventos WHERE Id = {id}"));
            Assert.Equal((int)EstadoEvento.Publicado, await CountAsync(
                $"SELECT Estado FROM fot_Eventos WHERE Id = {id}"));

            var dto = await (await client.GetAsync($"/api/fotos/eventos/{id}"))
                .Content.ReadFromJsonAsync<EventoDto>();
            Assert.Equal(2, dto!.TamanosPrecios.Count);
            Assert.Equal(1800, dto.TamanosPrecios.Single(t => t.Id == chico.Id).PrecioUnitario);
            Assert.DoesNotContain(dto.TamanosPrecios, t => t.Nombre == "13x18");
            Assert.Contains(dto.TamanosPrecios, t => t.Nombre == "20x30");
        }

        [Fact] // EVT-06 — validación de forma: nombre vacío → 400 (el validator corre en el update heredado)
        public async Task Nombre_vacio_da_400()
        {
            using var client = await CreateTenantClientAsync();

            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update", Input(""));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Eventos"));
        }

        [Fact] // EVT-07 — validación de forma: precio negativo en el catálogo → 400
        public async Task Precio_negativo_da_400()
        {
            using var client = await CreateTenantClientAsync();

            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                Input("Con precio malo", tamanos: new[] { Tamano("10x15", -1) }));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
        }

        [Fact] // EVT-08 — guard multi-tenant: root en su propio tenant NO persiste eventos
        public async Task Root_sin_simular_tenant_no_persiste()
        {
            using var root = await CreateAuthenticatedClientAsync(); // root, tenant 1, sin SimulatedTenant

            var resp = await root.PostAsJsonAsync("/api/fotos/eventos/update", Input("No debería"));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Eventos"));
        }

        [Fact] // EVT-09 — aislamiento multi-tenant: el evento de tenant 2 no existe para tenant 3
        public async Task Evento_de_otro_tenant_es_invisible()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var id = await CreateEventoAsync(tenant2, "Solo tenant 2");

            using var tenant3 = await CreateTenantClientAsync(3);

            var detalle = await tenant3.GetAsync($"/api/fotos/eventos/{id}");
            await detalle.ShouldBeStatus(HttpStatusCode.NotFound);

            var page = await (await tenant3.GetAsync("/api/fotos/eventos"))
                .Content.ReadFromJsonAsync<Page<EventoHeaderDto>>();
            Assert.Empty(page!.Items);
        }

        [Fact] // EVT-11 — asignar un perfil/opciones propios del tenant persiste los ids (spec 9.0/9.1)
        public async Task Asigna_perfil_y_opciones_propias_del_tenant()
        {
            using var client = await CreateTenantClientAsync();
            var perfilId = await CreatePerfilMarcaAguaAsync(client);
            var opcionesId = await CreateOpcionesPublicacionAsync(client);

            var resp = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                Input("Con override", perfilMarcaAguaId: perfilId, opcionesPublicacionId: opcionesId));
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<EventoDto>();
            Assert.Equal(perfilId, dto!.PerfilMarcaAguaId);
            Assert.Equal(opcionesId, dto.OpcionesPublicacionId);
        }

        [Fact] // EVT-12 — guard multi-tenant: un perfil/opciones de OTRO tenant no se puede asignar
        public async Task Perfil_o_opciones_de_otro_tenant_da_400()
        {
            using var tenant3 = await CreateTenantClientAsync(3);
            var perfilDeOtroTenant = await CreatePerfilMarcaAguaAsync(tenant3);
            var opcionesDeOtroTenant = await CreateOpcionesPublicacionAsync(tenant3);

            using var client = await CreateTenantClientAsync(); // tenant 2

            var respPerfil = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                Input("Con perfil ajeno", perfilMarcaAguaId: perfilDeOtroTenant));
            await respPerfil.ShouldBeStatus(HttpStatusCode.BadRequest);

            var respOpciones = await client.PostAsJsonAsync("/api/fotos/eventos/update",
                Input("Con opciones ajenas", opcionesPublicacionId: opcionesDeOtroTenant));
            await respOpciones.ShouldBeStatus(HttpStatusCode.BadRequest);

            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_Eventos WHERE TenantId = 2"));
        }

        [Fact] // EVT-10 — delete: el evento se borra y el catálogo cascadea
        public async Task Delete_cascadea_el_catalogo()
        {
            using var client = await CreateTenantClientAsync();
            var id = await CreateEventoAsync(client, "Descartable",
                tamanos: new[] { Tamano("10x15", 1500) });

            var resp = await client.DeleteAsync($"/api/fotos/eventos/{id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_Eventos WHERE Id = {id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_TamanosPrecios WHERE EventoId = {id}"));
        }
    }
}
