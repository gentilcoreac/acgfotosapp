using System;

namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    public class UsuarioHeaderProjection
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public long? TipoLicenciaActivaId { get; set; }
    }
}
