using System.Threading.Tasks;

namespace AcgFotos.Core.Storage
{
    /// <summary>
    /// Abstracción de almacenamiento de archivos. El consumidor no sabe si atrás hay
    /// FileSystem, blob en la base, Azure Blob, etc. La implementación se selecciona por
    /// config (Storage:Provider) y se registra en AutofacModuleBase.
    ///
    /// El <c>key</c> es la ruta relativa lógica del recurso, ej. "tenants/ACME/images/logo.png".
    /// Cada provider la traduce a su backend (path físico, columna de DB, blob path).
    ///
    /// Para agregar un provider nuevo (Azure, DB), ver la guía en Docs/cleanup-log.md
    /// (Fase E — Storage). Resumen: implementar esta interfaz, registrarla en el switch de
    /// AutofacModuleBase según Storage:Provider, y para recursos no servibles estáticamente
    /// (DB/Azure privados) sumar un endpoint GET /api/storage/{key} que use ReadAsync.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>Guarda el contenido bajo la key. contentType se usa por providers que
        /// guardan metadata (DB/Azure); FileSystem lo ignora. visibility define la zona.</summary>
        Task SaveAsync(string key, byte[] content, string contentType, StorageVisibility visibility);

        /// <summary>Lee el contenido binario de la key.</summary>
        Task<byte[]> ReadAsync(string key);

        /// <summary>Lee el contenido de la key como texto (UTF-8).</summary>
        Task<string> ReadTextAsync(string key);

        /// <summary>Borra la key exacta. No falla si no existe.</summary>
        Task DeleteAsync(string key);

        /// <summary>Borra todas las variantes de extensión de una key sin extensión.
        /// Ej. keyWithoutExtension="tenants/ACME/images/Logo" borra Logo.png, Logo.svg, etc.</summary>
        Task DeleteByPrefixAsync(string keyWithoutExtension);

        /// <summary>Indica si existe un recurso con esa key.</summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>URL accesible para el cliente. FileSystem devuelve la URL estática directa
        /// (servida por UseStaticFiles); DB/Azure devolverían la URL del endpoint o un SAS.</summary>
        string GetUrl(string key, StorageVisibility visibility);
    }
}
