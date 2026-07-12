using System;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {

    public class Auditoria : EntityBase {

        public DateTime FechaHora { get; set; }
        public double Duracion { get; set; }
        public string Servicio { get; set; }
        public string Metodo { get; set; }
        public string Parametros { get; set; }
        public long? UsuarioId { get; set; }
        /// <summary>
        /// Si la acción se hizo impersonando (ADR-0002), userId real de root que la ejecutó. NULL en
        /// requests normales. <see cref="UsuarioId"/> queda con el usuario destino (identidad efectiva).
        /// </summary>
        public long? ImpersonatedBy { get; set; }
        //public long? CuentaId { get; set; }
        public string HttpMethod { get; set; }
        public string RequestAbsolutePath { get; set; }
        public string ClientIP { get; set; }
        public string ClientUserAgent { get; set; }
        public string ResultStatusCode { get; set; }
        public string ResultContent { get; set; }
        public Usuario Usuario { get; set; }
    }
}
