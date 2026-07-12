using System;
using AcgFotos.Core.Application;
using AcgFotos.Core.Storage;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Metadata de un archivo de tenant para listado/respuesta. El binario no viaja acá;
    /// se accede por <see cref="Url"/> (estática si Public, endpoint con auth si Private).
    /// </summary>
    public class TenantFileDto : DtoBase
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Length { get; set; }
        public StorageVisibility Visibility { get; set; }
        public long? CreatedByUserId { get; set; }
        public DateTime CreateDatetime { get; set; }

        /// <summary>URL para acceder al archivo. La puebla el AppService.</summary>
        public string Url { get; set; }
    }
}
