using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Application.Dtos
{
    public class UsuarioHistorialDto : DtoBase
    {
        public long TenantId { get; set; }
        public long UsuarioId { get; set; }
        public string Periodo { get; set; }
        public string Nombre { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime FechaLastLogin { get; set; }
    }
}
