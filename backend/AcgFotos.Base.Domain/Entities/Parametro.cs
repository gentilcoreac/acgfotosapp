using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class Parametro : EntityBase
    {
        public string Nombre { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
        public long AplicacionId { get; set; }
        public Aplicacion Aplicacion { get; set; }
        public int TipoDato { get; set; } // Entero, Booleano, Texto
        public long? PermisoId { get; set; }
        public Permiso Permiso { get; set; }
    }
}
