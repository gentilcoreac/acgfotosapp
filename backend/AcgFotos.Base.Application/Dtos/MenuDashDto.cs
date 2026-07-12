using AcgFotos.Core.Application;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos {
    public class MenuDashDto: DtoBase {

        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public long? PermisoId { get; set; }
        
        public bool Estado { get; set; }
        public int Orden { get; set; }

        public string ImagenWeb { get; set; }

        /// <summary>Ruta del front a la que navega el acceso directo (p. ej. <c>/usuarios</c>). Null en
        /// menús contenedores sin pantalla propia. Lo auto-mapea Mapperly desde <c>Menu.RoutePath</c>.</summary>
        public string RoutePath { get; set; }

        public long? MenuPadreId { get; set; }
        public string MenuPadreNombre { get; set; }

        public bool VisibleDash { get; set; }
    }
}
