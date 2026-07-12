using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class RolHeaderDto : HeaderDtoBase
    {
        public string Descripcion { get; set; }
        public bool EsDefaultParaNuevoTenant { get; set; }
    }
}
