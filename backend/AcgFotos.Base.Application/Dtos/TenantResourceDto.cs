using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class TenantResourceDto : DtoBase
    {
        public string Base64 { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public string MimeType { get; set; }
    }
}
