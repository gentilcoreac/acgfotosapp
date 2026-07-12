using System;
using AcgFotos.Core.Domain;
using AcgFotos.Core.Storage;

namespace AcgFotos.Base.Domain.Entities
{
    /// <summary>
    /// Archivo genérico de un tenant. La metadata vive acá; el binario lo maneja
    /// IStorageProvider (filesystem/DB/Azure) identificado por StorageKey.
    /// </summary>
    public class TenantFile : MultiTenantEntityBase
    {
        public string FileName { get; set; }

        public string ContentType { get; set; }

        public long Length { get; set; }

        /// <summary>Key lógica en el IStorageProvider (ej. "private/{tenantId}/files/{guid}/{name}").</summary>
        public string StorageKey { get; set; }

        public StorageVisibility Visibility { get; set; }

        /// <summary>Usuario que subió el archivo (opcional — trazabilidad).</summary>
        public long? CreatedByUserId { get; set; }

        public DateTime CreateDatetime { get; set; }
    }
}
