using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AcgFotos.Core.Storage
{
    /// <summary>
    /// Provider de storage sobre el filesystem local/red. Dos zonas según la key:
    ///   - Keys que empiezan con "private/" → raíz privada FUERA de wwwroot
    ///     ({ContentRoot}/App_Data/storage). NO servible por UseStaticFiles; el contenido
    ///     se entrega por endpoint con [Authorize]. GetUrl(Private) lanza NotSupported.
    ///   - Resto (ej. branding "tenants/...") → wwwroot/assets, servido por UseStaticFiles.
    ///     GetUrl devuelve la URL estática directa.
    ///
    /// La zona se infiere del prefijo de la key (no del parámetro visibility), así
    /// ReadAsync/DeleteAsync —que solo reciben key— resuelven la ubicación correcta.
    /// </summary>
    public class FileSystemStorageProvider : IStorageProvider
    {
        private const string PublicAssetsFolder = "assets";
        private const string PrivatePrefix = "private/";

        private readonly string _publicRoot;
        private readonly string _privateRoot;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FileSystemStorageProvider(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _publicRoot = Path.Combine(env.WebRootPath, PublicAssetsFolder);
            _privateRoot = Path.Combine(env.ContentRootPath, "App_Data", "storage");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SaveAsync(string key, byte[] content, string contentType, StorageVisibility visibility)
        {
            var fullPath = this.ResolvePath(key);
            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllBytesAsync(fullPath, content);
        }

        public Task<byte[]> ReadAsync(string key) =>
            File.ReadAllBytesAsync(this.ResolvePath(key));

        public Task<string> ReadTextAsync(string key) =>
            File.ReadAllTextAsync(this.ResolvePath(key));

        public Task DeleteAsync(string key)
        {
            var fullPath = this.ResolvePath(key);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }

        public Task DeleteByPrefixAsync(string keyWithoutExtension)
        {
            var fullPath = this.ResolvePath(keyWithoutExtension);
            var dir = Path.GetDirectoryName(fullPath);
            if (Directory.Exists(dir))
            {
                var fileName = Path.GetFileName(fullPath);
                foreach (var file in Directory.GetFiles(dir, $"{fileName}.*"))
                {
                    File.Delete(file);
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key) =>
            Task.FromResult(File.Exists(this.ResolvePath(key)));

        public string GetUrl(string key, StorageVisibility visibility)
        {
            if (visibility == StorageVisibility.Private || this.IsPrivateKey(key))
            {
                throw new NotSupportedException(
                    "Los recursos privados no tienen URL estática; se sirven por endpoint con autorización.");
            }

            var request = _httpContextAccessor.HttpContext.Request;
            var normalized = key.Replace("\\", "/");
            return $"{request.Scheme}://{request.Host}/{PublicAssetsFolder}/{normalized}";
        }

        private bool IsPrivateKey(string key) =>
            key.Replace("\\", "/").StartsWith(PrivatePrefix, StringComparison.OrdinalIgnoreCase);

        private string ResolvePath(string key)
        {
            var normalized = key.Replace("\\", "/");
            if (this.IsPrivateKey(normalized))
            {
                // Saca el prefijo "private/" — la raíz privada ya representa esa zona.
                var relative = normalized.Substring(PrivatePrefix.Length)
                    .Replace("/", Path.DirectorySeparatorChar.ToString());
                return Path.Combine(_privateRoot, relative);
            }

            var publicRelative = normalized.Replace("/", Path.DirectorySeparatorChar.ToString());
            return Path.Combine(_publicRoot, publicRelative);
        }
    }
}
