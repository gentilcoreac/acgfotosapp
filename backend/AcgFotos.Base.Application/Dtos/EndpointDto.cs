using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class EndpointDto : DtoBase
    {
        public string Namespace { get; set; }
        public string ModuleName { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string HttpMethod { get; set; }
        public string Route { get; set; }
        public bool Activo { get; set; }
        public string Descripcion { get => $"{this.ModuleName} - {this.ControllerName} - {this.ActionName} ({this.ActionName})"; }
    }
     
}
