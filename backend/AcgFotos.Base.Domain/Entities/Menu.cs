using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities {

    public class Menu : EntityBase {

        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public bool Estado { get; set; }
        public string ImagenWeb { get; set; }
        public int Orden { get; set; }
        public Menu MenuPadre { get; set; }
        public long? MenuPadreId { get; set; }
        public Permiso Permiso { get; set; }
        public long? PermisoId { get; set; }

        public long? AplicacionId { get; set; }
        public Aplicacion Aplicacion { get; set; }

        public bool VisibleSideMenu { get; set; }

        public bool VisibleDash { get; set; }
        
        public string RoutePath { get; set; }

        public virtual ICollection<Menu> MenuHijos { get; set; }

        public Menu() {
            this.MenuHijos = new List<Menu>();
        }
   }
}

