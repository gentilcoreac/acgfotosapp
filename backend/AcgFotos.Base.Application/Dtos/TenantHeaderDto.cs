using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Shape liviano para listados/grillas/selectores de Tenant. Sin colecciones ni navegaciones.
    /// Los DTOs de detail/edit heredan transitivamente de éste vía TenantHeaderStyleDto.
    /// </summary>
    public class TenantHeaderDto : HeaderDtoBase
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string TituloWeb { get; set; }
        public bool Activo { get; set; }
        public string HostName { get; set; }
    }
}
