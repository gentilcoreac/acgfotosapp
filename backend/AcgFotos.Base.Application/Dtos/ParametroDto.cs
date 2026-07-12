using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class ParametroDto : DtoBase
    {
        public string Nombre { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
        public long AplicacionId { get; set; }
        public string AplicacionNombre { get; set; }

        public int TipoDato { get; set; } // Entero, Booleano, Texto
        public long? PermisoId { get; set; }
    }
}
