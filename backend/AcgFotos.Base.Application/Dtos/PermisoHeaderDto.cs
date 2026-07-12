using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class PermisoHeaderDto : HeaderDtoBase
    {
        public string Nombre { get; set; }
        public string CodigoPermiso { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
        public string AplicacionDescripcion { get; set; }
        public string PermisoPadreDescripcion { get; set; }
    }
}
