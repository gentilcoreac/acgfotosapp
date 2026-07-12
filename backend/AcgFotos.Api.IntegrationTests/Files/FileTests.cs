using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Files
{
    /// <summary>
    /// Archivos (300) — gestor genérico por tenant sobre IStorageProvider. `TenantFile` es multi-tenant
    /// (filtro global) → listado/descarga/borrado se aíslan solos por el TenantId del contexto. Foco:
    /// upload happy + validación + **aislamiento cross-tenant** (no listar / no descargar / no borrar archivos
    /// de otro tenant). I/O real de storage en el host de tests. Actores: userb (t2), adminb (t3).
    /// </summary>
    public class FileTests : IntegrationTestBase
    {
        public FileTests(TestWebApplicationFactory factory) : base(factory) { }

        private sealed record FileItem(long Id, string FileName);

        private static async Task<long> UploadAsync(HttpClient client, string fileName, byte[]? bytes = null, string visibility = "Private")
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes ?? new byte[] { 1, 2, 3, 4 });
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", fileName);

            var resp = await client.PostAsync($"/api/general/files?visibility={visibility}", form);
            await resp.ShouldBeOk();
            return (await resp.Content.ReadFromJsonAsync<FileItem>())!.Id;
        }

        [Fact] // FILE-01 — upload privado happy: persiste el TenantFile en el tenant del contexto
        public async Task Upload_privado_happy()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB); // t2
            var id = await UploadAsync(client, "a.txt");

            Assert.True(id > 0);
            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TenantFiles WHERE Id = {id} AND TenantId = {TestData.ActiveTenantId}"));
        }

        [Fact] // FILE-04 — upload de archivo de 0 bytes: 400 (chequeo explícito file.Length==0)
        public async Task Upload_archivo_vacio_da_400()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.Array.Empty<byte>());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", "vacio.txt");

            var resp = await client.PostAsync("/api/general/files", form);

            await resp.ShouldBeStatus(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact] // FILE-06 — el listado trae SOLO los archivos del tenant del caller (aislamiento)
        public async Task Listado_solo_del_tenant()
        {
            using var clientB = await CreateAuthenticatedClientAsync(TestData.UserB);  // t2
            await UploadAsync(clientB, "deB.txt");
            using var clientC = await CreateAuthenticatedClientAsync(TestData.AdminB);  // t3
            await UploadAsync(clientC, "deC.txt");

            var listB = await (await clientB.GetAsync("/api/general/files")).Content.ReadFromJsonAsync<System.Collections.Generic.List<FileItem>>();

            Assert.Contains(listB!, f => f.FileName == "deB.txt");
            Assert.DoesNotContain(listB!, f => f.FileName == "deC.txt"); // archivo de t3 no se filtra
        }

        [Fact] // FILE-07 — download privado con auth: devuelve el binario subido
        public async Task Download_privado_devuelve_binario()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var id = await UploadAsync(client, "bin.dat", new byte[] { 9, 8, 7, 6, 5 });

            var resp = await client.GetAsync($"/api/general/files/{id}/download");
            await resp.ShouldBeOk();

            Assert.Equal(new byte[] { 9, 8, 7, 6, 5 }, await resp.Content.ReadAsByteArrayAsync());
        }

        [Fact] // FILE-08 — download de un archivo de OTRO tenant: no se sirve (GetByIdAsync filtrado → null)
        public async Task Download_cross_tenant_no_sirve()
        {
            using var clientC = await CreateAuthenticatedClientAsync(TestData.AdminB); // t3
            var idC = await UploadAsync(clientC, "secretoC.dat", new byte[] { 1, 1, 1 });

            using var clientB = await CreateAuthenticatedClientAsync(TestData.UserB);  // t2
            var resp = await clientB.GetAsync($"/api/general/files/{idC}/download");

            Assert.False(resp.IsSuccessStatusCode); // no entrega el binario ajeno
        }

        [Fact] // FILE-10 — delete del propio tenant: deja de listarse
        public async Task Delete_propio()
        {
            using var client = await CreateAuthenticatedClientAsync(TestData.UserB);
            var id = await UploadAsync(client, "borrar.txt");

            await (await client.DeleteAsync($"/api/general/files/{id}")).ShouldBeOk();

            Assert.Equal(0, await CountAsync($"SELECT COUNT(*) FROM gen_TenantFiles WHERE Id = {id}"));
        }

        [Fact] // FILE-11 — delete de un archivo de OTRO tenant: no borra (filtro global lo oculta)
        public async Task Delete_cross_tenant_no_borra()
        {
            using var clientC = await CreateAuthenticatedClientAsync(TestData.AdminB); // t3
            var idC = await UploadAsync(clientC, "ajenoC.txt");

            using var clientB = await CreateAuthenticatedClientAsync(TestData.UserB);  // t2
            await clientB.DeleteAsync($"/api/general/files/{idC}");

            Assert.Equal(1, await CountAsync($"SELECT COUNT(*) FROM gen_TenantFiles WHERE Id = {idC}")); // sobrevive
        }
    }
}
