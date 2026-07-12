using System.IO;

namespace AcgFotos.Core.Storage
{
    /// <summary>
    /// Infiere el Content-Type a partir de la extensión del archivo. Lo usa el AppService
    /// al guardar (los providers DB/Azure lo persisten como metadata para servir con el
    /// header correcto; FileSystem lo ignora porque UseStaticFiles lo resuelve solo).
    /// </summary>
    public static class StorageContentTypes
    {
        public static string FromFileName(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".webp" => "image/webp",
                ".css" => "text/css",
                _ => "application/octet-stream"
            };
        }
    }
}
