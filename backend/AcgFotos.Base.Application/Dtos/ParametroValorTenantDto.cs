using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class ParametroValorTenantDto : DtoBase
    {
        public string Valor { get; set; }
        public long TenantId { get; set; }
        public long ParametroId { get; set; }
    }
}
