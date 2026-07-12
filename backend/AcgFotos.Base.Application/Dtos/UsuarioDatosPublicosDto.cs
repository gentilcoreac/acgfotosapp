using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{

    public class UsuarioDatosPublicosDto :  DtoBase {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
    }
}
