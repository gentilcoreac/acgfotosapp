using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class PermisoEndpoint : EntityBase {
        public long PermisoId { get; set; }

        public long EndpointId { get; set; }
        public Endpoint Endpoint { get; set; }
    }
}
