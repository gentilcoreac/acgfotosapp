using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Application.Dtos
{
    public class UsuariosActivosMensualDto : DtoBase
    {
        public string Periodo { get; set; }
        public int Cantidad { get; set; }
        public virtual List<UsuarioHistorialDto> UsuariosHistorial { get; set; }
    }
}
