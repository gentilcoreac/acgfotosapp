using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class GrupoHeaderDto : HeaderDtoBase
    {
        public string Nombre { get; set; }
        public int CantidadMiembros { get; set; }
    }
}
