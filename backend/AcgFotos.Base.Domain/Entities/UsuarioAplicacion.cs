using AcgFotos.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Domain.Entities
{
    public class UsuarioAplicacion : MultiTenantEntityBase
    {
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
        public long AplicacionId { get; set; }
        public Aplicacion Aplicacion { get; set; }
        public bool Default { get; set; }
    }
}
