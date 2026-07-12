using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Storage;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;

namespace AcgFotos.Base.Application.IServices
{
    /// <summary>
    /// Gestor de archivos genéricos por tenant, sobre IStorageProvider. Los archivos
    /// pertenecen al tenant del caller (aislamiento por el filtro global de TenantId).
    /// </summary>
    public interface IFileAppService
    {
        /// <summary>Sube un archivo. Devuelve su metadata (con Url).</summary>
        Task<TenantFileDto> UploadAsync(byte[] content, string fileName, string contentType, StorageVisibility visibility);

        /// <summary>Lista los archivos del tenant del caller.</summary>
        Task<List<TenantFileDto>> ListAsync();

        /// <summary>Devuelve el binario para descargar. 404 si no existe o es de otro tenant.</summary>
        Task<FileDownloadResult> GetForDownloadAsync(long id);

        /// <summary>Borra el archivo (storage + metadata). 404 si no existe o es de otro tenant.</summary>
        Task DeleteAsync(long id);
    }
}
