using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class PermisoEndpointDto : DtoBase
    {
        public long PermisoId { get; set; }

        public long EndpointId { get; set; }

        public string Descripcion { get; set; }
        public string EndpointModuleName { get; set; }
        public string EndpointControllerName { get; set; }
        public string EndpointRoute { get; set; }
        public string EndpointHttpMethodRoute { get; set; }
    }
}
