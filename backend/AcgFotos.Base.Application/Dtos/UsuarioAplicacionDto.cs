using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class UsuarioAplicacionDto : DtoBase
    {
        public long UsuarioId { get; set; }
        public UsuarioDto Usuario { get; set; }
        public long AplicacionId { get; set; }
        public AplicacionDto Aplicacion { get; set; }
        public string AplicacionNombre { get; set; }
        public bool Default { get; set; }
    }
}
