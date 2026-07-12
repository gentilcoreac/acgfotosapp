namespace AcgFotos.Core.Security
{
    public class EndpointDto
    {
        public string ActionName { get; set; }

        public string ControllerName { get; set; }

        public string ModuleName { get; set; }

        public string Namespace { get; set; }

        public string Route { get; set; }

        public string HttpMethod { get; set; }

        public bool Activo { get; set; }
    }
}
