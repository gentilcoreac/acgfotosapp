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
    /// CRUD de perfiles de marca de agua (ADR-15). El alta de la primera capa crea el perfil
    /// (design.md D14): no hay un endpoint de "crear perfil vacío", así que estos tests arrancan
    /// siempre por <c>SubirCapa</c>.
    /// </summary>
    public class PerfilMarcaAguaCrudTests : IntegrationTestBase
    {
        public PerfilMarcaAguaCrudTests(TestWebApplicationFactory factory) : base(factory) { }

        private async Task<HttpClient> CreateTenantClientAsync(long tenantId = 2)
        {
            var client = await CreateAuthenticatedClientAsync(); // root
            client.DefaultRequestHeaders.Add("SimulatedTenant", tenantId.ToString());
            return client;
        }

        private static byte[] PngConAlfa(int ancho = 80, int alto = 60)
        {
            using var img = new Image<Rgba32>(ancho, alto, new Rgba32(255, 255, 255, 128));
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        private static MultipartFormDataContent SubidaCapaContent(
            byte[] contenido, long? perfilId = null, string? nombrePerfilSiNuevo = null)
        {
            var multipart = new MultipartFormDataContent();
            if (perfilId is long id)
            {
                multipart.Add(new StringContent(id.ToString()), "perfilMarcaAguaId");
            }

            if (nombrePerfilSiNuevo != null)
            {
                multipart.Add(new StringContent(nombrePerfilSiNuevo), "nombrePerfilSiNuevo");
            }

            multipart.Add(new ByteArrayContent(contenido), "archivo", "capa.png");
            return multipart;
        }

        private async Task<CapaMarcaAguaSubidaDto> SubirCapaAsync(
            HttpClient client, long? perfilId = null, string? nombre = null, byte[]? contenido = null)
        {
            var resp = await client.PostAsync(
                "/api/fotos/marca-agua/perfiles/capas/upload",
                SubidaCapaContent(contenido ?? PngConAlfa(), perfilId, nombre));
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<CapaMarcaAguaSubidaDto>())!;
        }

        private static object InputEdicion(long id, string nombre, bool esDefault, bool marcarThumb,
            object[] capas) => new { id, nombre, esDefault, marcarThumb, capas };

        private static object Capa(CapaMarcaAguaDto capa, ModoColocacionMarcaAgua? modoColocacion = null,
            PosicionMarcaAgua? posicion = null, float? opacidad = null) => new
        {
            id = capa.Id,
            orden = capa.Orden,
            modoColocacion = (int)(modoColocacion ?? capa.ModoColocacion),
            posicion = posicion.HasValue ? (int)posicion.Value
                : capa.Posicion.HasValue ? (int)capa.Posicion.Value : (int?)null,
            escalaPorcentaje = capa.EscalaPorcentaje,
            margenPorcentaje = capa.MargenPorcentaje,
            separacionPorcentaje = capa.SeparacionPorcentaje,
            anguloGrados = capa.AnguloGrados,
            opacidad = opacidad ?? capa.Opacidad,
            modoFusion = (int)capa.ModoFusion,
        };

        [Fact] // PMA-01 — subir la primera capa sin perfilId crea el perfil
        public async Task Subir_primera_capa_crea_el_perfil()
        {
            using var client = await CreateTenantClientAsync();

            var subida = await SubirCapaAsync(client, nombre: "Mi marca");

            Assert.Equal("Mi marca", subida.Perfil.Nombre);
            var capa = Assert.Single(subida.Perfil.Capas);
            Assert.Equal(capa.Id, subida.Capa.Id);
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_PerfilesMarcaAgua WHERE Id = {subida.Perfil.Id} AND TenantId = 2"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_CapasMarcaAgua WHERE PerfilMarcaAguaId = {subida.Perfil.Id}"));
        }

        [Fact] // PMA-02 — subir con perfilId existente agrega otra capa al mismo perfil
        public async Task Subir_con_perfil_existente_agrega_una_segunda_capa()
        {
            using var client = await CreateTenantClientAsync();
            var primera = await SubirCapaAsync(client, nombre: "Con logo y trama");

            var segunda = await SubirCapaAsync(client, perfilId: primera.Perfil.Id);

            Assert.Equal(2, segunda.Perfil.Capas.Count);
            Assert.Equal(2, await CountAsync($"SELECT COUNT(*) FROM fot_CapasMarcaAgua WHERE PerfilMarcaAguaId = {primera.Perfil.Id}"));
        }

        [Fact] // PMA-03 — un perfil no admite una 4ª capa
        public async Task Subir_una_cuarta_capa_es_rechazado()
        {
            using var client = await CreateTenantClientAsync();
            var perfil = (await SubirCapaAsync(client)).Perfil;
            perfil = (await SubirCapaAsync(client, perfilId: perfil.Id)).Perfil;
            perfil = (await SubirCapaAsync(client, perfilId: perfil.Id)).Perfil;

            var resp = await client.PostAsync(
                "/api/fotos/marca-agua/perfiles/capas/upload",
                SubidaCapaContent(PngConAlfa(), perfil.Id));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
            Assert.Equal(3, await CountAsync($"SELECT COUNT(*) FROM fot_CapasMarcaAgua WHERE PerfilMarcaAguaId = {perfil.Id}"));
        }

        [Fact] // PMA-04 — detalle trae las capas
        public async Task Detalle_trae_las_capas()
        {
            using var client = await CreateTenantClientAsync();
            var subida = await SubirCapaAsync(client, nombre: "Con detalle");

            var resp = await client.GetAsync($"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}");
            await resp.ShouldBeOk();

            var dto = await resp.Content.ReadFromJsonAsync<PerfilMarcaAguaDto>();
            Assert.Equal("Con detalle", dto!.Nombre);
            var capa = Assert.Single(dto.Capas);
            Assert.Equal(80, capa.AnchoPx);
            Assert.Equal(60, capa.AltoPx);
        }

        [Fact] // PMA-05 — edición: cambia nombre y colocación de una capa existente
        public async Task Edicion_actualiza_nombre_y_colocacion()
        {
            using var client = await CreateTenantClientAsync();
            var subida = await SubirCapaAsync(client, nombre: "Original");

            var resp = await client.PostAsJsonAsync("/api/fotos/marca-agua/perfiles/update",
                InputEdicion(subida.Perfil.Id, "Renombrado", esDefault: false, marcarThumb: false,
                    capas: new[]
                    {
                        Capa(subida.Capa, modoColocacion: ModoColocacionMarcaAgua.PosicionFija,
                            posicion: PosicionMarcaAgua.Centro),
                    }));
            await resp.ShouldBeOk();

            var dto = await (await client.GetAsync($"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}"))
                .Content.ReadFromJsonAsync<PerfilMarcaAguaDto>();
            Assert.Equal("Renombrado", dto!.Nombre);
            Assert.False(dto.MarcarThumb);
            Assert.Equal(ModoColocacionMarcaAgua.PosicionFija, dto.Capas.Single().ModoColocacion);
        }

        [Fact] // PMA-06 — una capa con Id 0 en el input de edición es rechazada (el alta va por SubirCapa)
        public async Task Edicion_con_capa_id_cero_da_400()
        {
            using var client = await CreateTenantClientAsync();
            var subida = await SubirCapaAsync(client);

            var resp = await client.PostAsJsonAsync("/api/fotos/marca-agua/perfiles/update",
                InputEdicion(subida.Perfil.Id, "Con capa nueva", esDefault: false, marcarThumb: true,
                    capas: new[]
                    {
                        Capa(subida.Capa),
                        new
                        {
                            id = 0L, orden = 1, modoColocacion = 0, posicion = (int?)null,
                            escalaPorcentaje = 20f, margenPorcentaje = 5f, anguloGrados = 0f,
                            opacidad = 1f, modoFusion = 0,
                        },
                    }));

            await resp.ShouldBeStatus(HttpStatusCode.BadRequest);
        }

        [Fact] // PMA-07 — quitar una capa del input la elimina de la base
        public async Task Edicion_sin_una_capa_la_elimina()
        {
            using var client = await CreateTenantClientAsync();
            var primera = await SubirCapaAsync(client);
            var segunda = await SubirCapaAsync(client, perfilId: primera.Perfil.Id);

            var resp = await client.PostAsJsonAsync("/api/fotos/marca-agua/perfiles/update",
                InputEdicion(primera.Perfil.Id, "Con una sola capa", esDefault: false, marcarThumb: true,
                    capas: new[] { Capa(segunda.Perfil.Capas.First(c => c.Id == primera.Capa.Id)) }));
            await resp.ShouldBeOk();

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_CapasMarcaAgua WHERE PerfilMarcaAguaId = {primera.Perfil.Id}"));
        }

        [Fact] // PMA-08 — un solo default por tenant: marcar uno nuevo desmarca el anterior
        public async Task Marcar_default_desmarca_el_anterior()
        {
            using var client = await CreateTenantClientAsync();
            var uno = await SubirCapaAsync(client, nombre: "Uno");
            var dos = await SubirCapaAsync(client, nombre: "Dos");

            await (await client.PostAsJsonAsync("/api/fotos/marca-agua/perfiles/update",
                InputEdicion(uno.Perfil.Id, "Uno", esDefault: true, marcarThumb: true,
                    capas: new[] { Capa(uno.Capa) }))).ShouldBeOk();

            await (await client.PostAsJsonAsync("/api/fotos/marca-agua/perfiles/update",
                InputEdicion(dos.Perfil.Id, "Dos", esDefault: true, marcarThumb: true,
                    capas: new[] { Capa(dos.Capa) }))).ShouldBeOk();

            Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM fot_PerfilesMarcaAgua WHERE TenantId = 2 AND EsDefault = true"));
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM fot_PerfilesMarcaAgua WHERE Id = {dos.Perfil.Id} AND EsDefault = true"));
        }

        [Fact] // PMA-09 — aviso al guardar un perfil sin protección efectiva (todas las capas en opacidad 0)
        public async Task Perfil_con_opacidad_nula_avisa_sin_proteccion()
        {
            using var client = await CreateTenantClientAsync();
            var subida = await SubirCapaAsync(client);

            var resp = await client.GetAsync($"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}");
            var dtoOriginal = await resp.Content.ReadFromJsonAsync<PerfilMarcaAguaDto>();
            Assert.Empty(dtoOriginal!.Avisos);

            await (await client.PostAsJsonAsync("/api/fotos/marca-agua/perfiles/update",
                InputEdicion(subida.Perfil.Id, "Sin protección", esDefault: false, marcarThumb: true,
                    capas: new[] { Capa(subida.Capa, opacidad: 0f) }))).ShouldBeOk();

            var dto = await (await client.GetAsync($"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}"))
                .Content.ReadFromJsonAsync<PerfilMarcaAguaDto>();
            Assert.Contains(dto!.Avisos, a => a.Contains("sin ninguna protección"));
        }

        [Fact] // PMA-10 — delete cascadea las capas
        public async Task Delete_cascadea_las_capas()
        {
            using var client = await CreateTenantClientAsync();
            var subida = await SubirCapaAsync(client);

            var resp = await client.DeleteAsync($"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}");
            await resp.ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_PerfilesMarcaAgua WHERE Id = {subida.Perfil.Id}"));
            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM fot_CapasMarcaAgua WHERE PerfilMarcaAguaId = {subida.Perfil.Id}"));
        }

        [Fact] // PMA-11 — aislamiento multi-tenant: el perfil de tenant 2 no existe para tenant 3
        public async Task Perfil_de_otro_tenant_es_invisible()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var subida = await SubirCapaAsync(tenant2);

            using var tenant3 = await CreateTenantClientAsync(3);

            await (await tenant3.GetAsync($"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}"))
                .ShouldBeStatus(HttpStatusCode.NotFound);

            // El listado no está vacío: el primer acceso del tenant siembra su propio perfil
            // "Estándar" (ADR-15 §1, design.md D11/5.9) — lo que importa es que NO aparezca el de tenant 2.
            var page = await (await tenant3.GetAsync("/api/fotos/marca-agua/perfiles"))
                .Content.ReadFromJsonAsync<Page<PerfilMarcaAguaDto>>();
            Assert.DoesNotContain(page!.Items, p => p.Id == subida.Perfil.Id);
        }

        [Fact] // PMA-12 — el asset de una capa se lee autenticado por su key
        public async Task Lee_el_asset_de_una_capa()
        {
            using var client = await CreateTenantClientAsync();
            var contenido = PngConAlfa(120, 90);
            var subida = await SubirCapaAsync(client, contenido: contenido);

            var resp = await client.GetAsync(
                $"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}/capas/{subida.Capa.StorageKey}");
            await resp.ShouldBeOk();

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            Assert.Equal(contenido, bytes);
        }

        [Fact] // PMA-13 — el asset de una capa de OTRO tenant no se puede leer
        public async Task Asset_de_otro_tenant_da_404()
        {
            using var tenant2 = await CreateTenantClientAsync(2);
            var subida = await SubirCapaAsync(tenant2);

            using var tenant3 = await CreateTenantClientAsync(3);

            await (await tenant3.GetAsync(
                    $"/api/fotos/marca-agua/perfiles/{subida.Perfil.Id}/capas/{subida.Capa.StorageKey}"))
                .ShouldBeStatus(HttpStatusCode.NotFound);
        }

        [Fact] // PMA-14 — el primer listado del tenant siembra el perfil "Estándar" (D11/5.9), sin marcarlo default
        public async Task Primer_listado_siembra_el_perfil_estandar()
        {
            using var client = await CreateTenantClientAsync();
            Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM fot_PerfilesMarcaAgua WHERE TenantId = 2"));

            var page = await (await client.GetAsync("/api/fotos/marca-agua/perfiles"))
                .Content.ReadFromJsonAsync<Page<PerfilMarcaAguaDto>>();

            var estandar = Assert.Single(page!.Items);
            Assert.Equal("Estándar", estandar.Nombre);
            Assert.False(estandar.EsDefault);
            var capa = Assert.Single(estandar.Capas);
            Assert.Equal(ModoColocacionMarcaAgua.Repetida, capa.ModoColocacion);

            // Idempotente: un segundo listado no crea un segundo perfil.
            await client.GetAsync("/api/fotos/marca-agua/perfiles");
            Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM fot_PerfilesMarcaAgua WHERE TenantId = 2"));
        }
    }
}
