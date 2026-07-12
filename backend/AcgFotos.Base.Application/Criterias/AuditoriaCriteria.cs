using System;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class AuditoriaCriteria : ListaPaginadaCriteriaBase {
        public long? UsuarioId { get; set; }
        public long? CuentaId { get; set; }

        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        public string Servicio { get; set; }
        public string Metodo { get; set; }

        public string ClientIP { get; set; }
        public string ClientUserAgent { get; set; }
        public string ResultStatusCode { get; set; }

        public bool IncluirParametros { get; set; }
        public bool IncluirResultado { get; set; }
    }
}
