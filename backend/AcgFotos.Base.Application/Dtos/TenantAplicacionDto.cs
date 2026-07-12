using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Application.Dtos
{
    public class TenantAplicacionDto : DtoBase
    {
        public long AplicacionId { get; set; }
        public string AplicacionNombre { get; set; }
        public long TenantId { get; set; }
    }
}
