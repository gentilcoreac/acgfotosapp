using AcgFotos.Core.Application;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos {
    public class MenuDto: DtoBase {

        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public bool Estado { get; set; }
        public string ImagenWeb { get; set; }
        public int Orden { get; set; }
        public long? MenuPadreId { get; set; }
        public long? PermisoId { get; set; }

        public string PermisoNombre{ get; set; }

        public virtual ICollection<MenuDto> MenuHijos { get; set; }

        public long? AplicacionId { get; set; }
        public string AplicacionDescripcion { get; set; }

        public bool VisibleSideMenu { get; set; }

        public bool VisibleDash { get; set; }

        public string RoutePath { get; set; }


        public MenuDto() {
            this.MenuHijos = new List<MenuDto>();
        }
    }
}
