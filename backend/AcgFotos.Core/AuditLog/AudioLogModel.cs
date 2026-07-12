using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Core.AuditLog
{
    public class AuditLogModel
    {
        public DateTime FechaHora { get; set; }
        public double Duracion { get; set; }
        public string Servicio { get; set; }
        public string Metodo { get; set; }
        public string Parametros { get; set; }
        public long? UsuarioId { get; set; }
        /// <summary>userId real de root cuando la acción se hizo impersonando (ADR-0002); NULL si no.</summary>
        public long? ImpersonatedBy { get; set; }
        public long? CuentaId { get; set; }
        public string HttpMethod { get; set; }
        public string RequestAbsolutePath { get; set; }
        public string ClientIP { get; set; }
        public string ClientUserAgent { get; set; }
        public string ResultStatusCode { get; set; }
        public string ResultContent { get; set; }
        public string UsuarioNombre { get; set; }
        public string CuentaNombre { get; set; }
    }
}
